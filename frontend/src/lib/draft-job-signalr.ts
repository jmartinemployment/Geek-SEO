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
import { SEO_HUB_EVENTS } from '@/lib/seo-hub-events';

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

type BeginDraftJobWaitOptions = {
  hub: SeoHubApi;
  accessToken?: string | null;
  timeoutMs?: number;
  onProgress?: (status: BackgroundJobStatus, elapsedMs: number) => void;
};

type DraftJobWaitHandle = {
  /** Subscribe handlers, then block until the shared hub connection is ready. Call before enqueue. */
  whenReady: () => Promise<void>;
  /** Arm jobId filter and wait for terminal status. Call immediately after enqueue returns. */
  waitFor: (jobId: string) => Promise<BackgroundJobStatus>;
  dispose: () => void;
};

function beginDraftJobWait(options: BeginDraftJobWaitOptions): DraftJobWaitHandle {
  const { hub, accessToken, onProgress } = options;
  const timeoutMs = options.timeoutMs ?? 20 * 60 * 1000;
  const startedAt = Date.now();

  let armed = false;
  let expectedJobId = '';
  let settled = false;
  let timeoutId: ReturnType<typeof setTimeout> | null = null;
  let resolveWait: ((status: BackgroundJobStatus) => void) | null = null;
  let rejectWait: ((error: Error) => void) | null = null;

  const finish = (status: BackgroundJobStatus) => {
    if (settled) return;
    settled = true;
    if (timeoutId) clearTimeout(timeoutId);
    resolveWait?.(status);
  };

  const fail = (error: Error) => {
    if (settled) return;
    settled = true;
    if (timeoutId) clearTimeout(timeoutId);
    rejectWait?.(error);
  };

  const onProgressMsg = (raw: unknown) => {
    if (!armed) return;
    const msg = raw as DraftJobMsg;
    const incomingJobId = msgJobId(msg);
    if (!incomingJobId || !idsMatch(incomingJobId, expectedJobId)) return;
    onProgress?.(toStatus(msg), Date.now() - startedAt);
  };

  const onCompleteMsg = (raw: unknown) => {
    if (!armed) return;
    const msg = raw as DraftJobMsg;
    const incomingJobId = msgJobId(msg);
    if (!incomingJobId || !idsMatch(incomingJobId, expectedJobId)) return;
    const status = toStatus(msg);
    onProgress?.(status, Date.now() - startedAt);
    if (TERMINAL.has(status.status)) {
      void (async () => {
        try {
          finish(await getBackgroundJob(expectedJobId, accessToken));
        } catch {
          finish(status);
        }
      })();
    }
  };

  const cleanupProgress = hub.subscribe(SEO_HUB_EVENTS.draftJobProgress, onProgressMsg);
  const cleanupComplete = hub.subscribe(SEO_HUB_EVENTS.draftJobComplete, onCompleteMsg);

  const dispose = () => {
    armed = false;
    if (timeoutId) clearTimeout(timeoutId);
    cleanupProgress();
    cleanupComplete();
  };

  return {
    whenReady: () => hub.whenConnected(),

    waitFor(jobId: string) {
      armed = true;
      expectedJobId = jobId;
      settled = false;

      return new Promise<BackgroundJobStatus>((resolve, reject) => {
        resolveWait = resolve;
        rejectWait = reject;

        void (async () => {
          const initial = await getBackgroundJob(jobId, accessToken);
          if (TERMINAL.has(initial.status.toLowerCase())) {
            onProgress?.(initial, Date.now() - startedAt);
            finish(initial);
            return;
          }

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
        })().catch((e) => {
          fail(e instanceof Error ? e : new Error('Failed waiting for draft job.'));
        });
      });
    },

    dispose,
  };
}

export async function waitForDraftJobViaSignalR(
  options: WaitForDraftJobOptions,
): Promise<BackgroundJobStatus> {
  const listener = beginDraftJobWait(options);
  try {
    await listener.whenReady();
    return await listener.waitFor(options.jobId);
  } finally {
    listener.dispose();
  }
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

async function runDraftJobWithListener<T>(
  hub: SeoHubApi,
  accessToken: string | null | undefined,
  onProgress: ((status: BackgroundJobStatus, elapsedMs: number) => void) | undefined,
  enqueue: () => Promise<BackgroundJobStatus>,
  hydrate: (terminal: BackgroundJobStatus) => Promise<T>,
): Promise<T> {
  const listener = beginDraftJobWait({ hub, accessToken, onProgress });
  try {
    await listener.whenReady();
    const enqueued = await enqueue();
    const terminal = await listener.waitFor(enqueued.jobId);
    return await hydrate(terminal);
  } finally {
    listener.dispose();
  }
}

export async function runKeywordContentDraft(
  documentId: string,
  _projectId: string,
  body: { keyword: string; location?: string; title?: string },
  accessToken?: string | null,
  options?: DraftJobProgressOptions,
): Promise<SeoContentDocument> {
  if (!options?.hub) throw new Error('SignalR hub is required for draft generation.');
  return runDraftJobWithListener(
    options.hub,
    accessToken,
    options.onProgress,
    () => enqueueKeywordContentDraft(documentId, body, accessToken),
    (terminal) => hydrateDraftDocument(terminal, documentId, accessToken),
  );
}

export async function runResearchContentDraft(
  documentId: string,
  accessToken?: string | null,
  options?: DraftJobProgressOptions,
): Promise<SeoContentDocument> {
  if (!options?.hub) throw new Error('SignalR hub is required for draft generation.');
  return runDraftJobWithListener(
    options.hub,
    accessToken,
    options.onProgress,
    () => enqueueResearchContentDraft(documentId, accessToken),
    (terminal) => hydrateDraftDocument(terminal, documentId, accessToken),
  );
}

export async function runBulkKeywordDrafts(
  projectId: string,
  keywords: string[],
  location: string,
  accessToken?: string | null,
  options?: { hub: SeoHubApi; onProgress?: (progress: BulkDraftProgress) => void },
): Promise<SeoContentDocument[]> {
  if (!options?.hub) throw new Error('SignalR hub is required for bulk draft generation.');
  await runDraftJobWithListener(
    options.hub,
    accessToken,
    (status, elapsedMs) => {
      options?.onProgress?.({
        keywordIndex: status.keywordIndex ?? 0,
        keywordTotal: status.keywordTotal ?? keywords.length,
        keyword: status.keyword ?? '',
        step: status,
        elapsedMs,
      });
    },
    () => enqueueBulkArticles({ projectId, keywords, location }, accessToken),
    async (terminal) => terminal,
  );

  const all = await listContent(projectId, accessToken);
  return all.filter((doc) => doc.status === 'awaiting_review' || doc.status === 'draft').slice(0, keywords.length);
}

export { describeDraftJobProgress, beginDraftJobWait };
