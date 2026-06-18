import {
  describeDraftJobProgress,
  enqueueBulkArticles,
  enqueueKeywordContentDraft,
  enqueueResearchContentDraft,
  getBackgroundJob,
  getContent,
  listContent,
  type BackgroundJobStatus,
  type SeoContentDocument,
} from '@/lib/seo-api';
import type { SeoHubApi } from '@/components/signalr/seo-hub-provider';

const TERMINAL = new Set(['completed', 'complete', 'failed']);

type DraftJobMsg = {
  jobId?: string;
  JobId?: string;
  jobType?: string;
  JobType?: string;
  status?: string;
  Status?: string;
  progressPercent?: number;
  ProgressPercent?: number;
  resultId?: string;
  ResultId?: string;
  errorMessage?: string;
  ErrorMessage?: string;
  keyword?: string;
  Keyword?: string;
  keywordIndex?: number;
  KeywordIndex?: number;
  keywordTotal?: number;
  KeywordTotal?: number;
  documentId?: string;
  DocumentId?: string;
};

function msgJobId(msg: DraftJobMsg): string | undefined {
  return msg.jobId ?? msg.JobId;
}

function toStatus(msg: DraftJobMsg): BackgroundJobStatus {
  return {
    jobId: msgJobId(msg) ?? '',
    jobType: msg.jobType ?? msg.JobType ?? 'content_draft',
    status: (msg.status ?? msg.Status ?? 'running').toLowerCase(),
    progressPercent: msg.progressPercent ?? msg.ProgressPercent ?? 0,
    resultId: msg.resultId ?? msg.ResultId,
    errorMessage: msg.errorMessage ?? msg.ErrorMessage,
    keyword: msg.keyword ?? msg.Keyword,
    keywordIndex: msg.keywordIndex ?? msg.KeywordIndex,
    keywordTotal: msg.keywordTotal ?? msg.KeywordTotal,
    documentId: msg.documentId ?? msg.DocumentId,
  };
}

function idsMatch(a: string | undefined, b: string): boolean {
  const left = a?.toLowerCase();
  const right = b.toLowerCase();
  return !left || left === right;
}

export type DraftJobProgressOptions = {
  hub: SeoHubApi;
  onProgress?: (status: BackgroundJobStatus, elapsedMs: number) => void;
};

export type WaitForDraftJobOptions = {
  jobId: string;
  hub: SeoHubApi;
  accessToken?: string | null;
  timeoutMs?: number;
  onProgress?: (status: BackgroundJobStatus, elapsedMs: number) => void;
};

export async function waitForDraftJobViaSignalR(
  options: WaitForDraftJobOptions,
): Promise<BackgroundJobStatus> {
  const { jobId, hub, accessToken, onProgress } = options;
  const timeoutMs = options.timeoutMs ?? 20 * 60 * 1000;
  const startedAt = Date.now();

  const initial = await getBackgroundJob(jobId, accessToken);
  if (TERMINAL.has(initial.status.toLowerCase())) {
    onProgress?.(initial, Date.now() - startedAt);
    return initial;
  }

  return new Promise((resolve, reject) => {
    let settled = false;
    let timeoutId: ReturnType<typeof setTimeout> | null = null;
    let cleanupProgress: (() => void) | undefined;
    let cleanupComplete: (() => void) | undefined;

    const finish = (status: BackgroundJobStatus) => {
      if (settled) return;
      settled = true;
      if (timeoutId) clearTimeout(timeoutId);
      cleanupProgress?.();
      cleanupComplete?.();
      resolve(status);
    };

    const fail = (error: Error) => {
      if (settled) return;
      settled = true;
      if (timeoutId) clearTimeout(timeoutId);
      cleanupProgress?.();
      cleanupComplete?.();
      reject(error);
    };

    const onProgressMsg = (raw: unknown) => {
      const msg = raw as DraftJobMsg;
      if (!idsMatch(msgJobId(msg), jobId)) return;
      onProgress?.(toStatus(msg), Date.now() - startedAt);
    };

    const onCompleteMsg = (raw: unknown) => {
      const msg = raw as DraftJobMsg;
      if (!idsMatch(msgJobId(msg), jobId)) return;
      const status = toStatus(msg);
      onProgress?.(status, Date.now() - startedAt);
      if (TERMINAL.has(status.status)) {
        void (async () => {
          try {
            finish(await getBackgroundJob(jobId, accessToken));
          } catch {
            finish(status);
          }
        })();
      }
    };

    cleanupProgress = hub.subscribe('DraftJobProgress', onProgressMsg);
    cleanupComplete = hub.subscribe('DraftJobComplete', onCompleteMsg);

    timeoutId = setTimeout(() => {
      void (async () => {
        try {
          const status = await getBackgroundJob(jobId, accessToken);
          if (TERMINAL.has(status.status.toLowerCase())) {
            finish(status);
            return;
          }
          fail(new Error('Timed out waiting for draft job to finish.'));
        } catch (e) {
          fail(e instanceof Error ? e : new Error('Timed out waiting for draft job.'));
        }
      })();
    }, timeoutMs);
  });
}

export async function hydrateDraftDocument(
  status: BackgroundJobStatus,
  fallbackDocumentId: string,
  accessToken?: string | null,
): Promise<SeoContentDocument> {
  if (status.status === 'failed') {
    throw new Error(status.errorMessage ?? 'Draft failed');
  }
  const docId = status.resultId ?? fallbackDocumentId;
  return getContent(docId, accessToken);
}

export type BulkDraftProgress = {
  keywordIndex: number;
  keywordTotal: number;
  keyword: string;
  step: BackgroundJobStatus;
  elapsedMs: number;
};

export async function runKeywordContentDraft(
  documentId: string,
  _projectId: string,
  body: { keyword: string; location?: string; title?: string },
  accessToken?: string | null,
  options?: DraftJobProgressOptions,
): Promise<SeoContentDocument> {
  if (!options?.hub) throw new Error('SignalR hub is required for draft generation.');
  const enqueued = await enqueueKeywordContentDraft(documentId, body, accessToken);
  const terminal = await waitForDraftJobViaSignalR({
    jobId: enqueued.jobId,
    hub: options.hub,
    accessToken,
    onProgress: options.onProgress,
  });
  return hydrateDraftDocument(terminal, documentId, accessToken);
}

export async function runResearchContentDraft(
  documentId: string,
  accessToken?: string | null,
  options?: DraftJobProgressOptions,
): Promise<SeoContentDocument> {
  if (!options?.hub) throw new Error('SignalR hub is required for draft generation.');
  const enqueued = await enqueueResearchContentDraft(documentId, accessToken);
  const terminal = await waitForDraftJobViaSignalR({
    jobId: enqueued.jobId,
    hub: options.hub,
    accessToken,
    onProgress: options.onProgress,
  });
  return hydrateDraftDocument(terminal, documentId, accessToken);
}

export async function runBulkKeywordDrafts(
  projectId: string,
  keywords: string[],
  location: string,
  accessToken?: string | null,
  options?: { hub: SeoHubApi; onProgress?: (progress: BulkDraftProgress) => void },
): Promise<SeoContentDocument[]> {
  if (!options?.hub) throw new Error('SignalR hub is required for bulk draft generation.');
  const enqueued = await enqueueBulkArticles({ projectId, keywords, location }, accessToken);
  await waitForDraftJobViaSignalR({
    jobId: enqueued.jobId,
    hub: options.hub,
    accessToken,
    onProgress: (status, elapsedMs) => {
      options?.onProgress?.({
        keywordIndex: status.keywordIndex ?? 0,
        keywordTotal: status.keywordTotal ?? keywords.length,
        keyword: status.keyword ?? '',
        step: status,
        elapsedMs,
      });
    },
  });

  const all = await listContent(projectId, accessToken);
  return all.filter((doc) => doc.status === 'awaiting_review' || doc.status === 'draft').slice(0, keywords.length);
}

export { describeDraftJobProgress };
