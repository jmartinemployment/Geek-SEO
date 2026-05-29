'use client';

import Link from 'next/link';
import { useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  createProject,
  getJobStatus,
  publishToWordPress,
  researchKeywords,
  startFullArticle,
  type BackgroundJobStatus,
  type KeywordResult,
  type SeoProject,
} from '@/lib/seo-api';

const STEPS = [
  'Business',
  'Keyword',
  'Generate',
  'Review',
  'Checklist',
  'Done',
] as const;

export default function GuidedWizardPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [step, setStep] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState('');
  const [url, setUrl] = useState('https://');
  const [location, setLocation] = useState('United States');
  const [project, setProject] = useState<SeoProject | null>(null);
  const [keyword, setKeyword] = useState('');
  const [job, setJob] = useState<BackgroundJobStatus | null>(null);
  const [documentId, setDocumentId] = useState<string | null>(null);
  const [ideas, setIdeas] = useState<KeywordResult[]>([]);
  const [ideasLoading, setIdeasLoading] = useState(false);
  const [publishUrl, setPublishUrl] = useState<string | null>(null);

  if (authLoading) return <main className="p-8">Loading…</main>;

  async function onCreateProject() {
    setError(null);
    try {
      const p = await createProject({ name, url, defaultLocation: location }, accessToken);
      setProject(p);
      setStep(1);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to create project');
    }
  }

  async function onGenerate() {
    if (!project) return;
    setError(null);
    setStep(2);
    try {
      const status = await startFullArticle(
        {
          projectId: project.id,
          keyword,
          location,
          title: keyword,
        },
        accessToken,
      );
      setJob(status);
      await pollJob(status.jobId);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Generation failed');
      setStep(1);
    }
  }

  async function pollJob(jobId: string) {
    const maxAttempts = 60;
    for (let i = 0; i < maxAttempts; i++) {
      await new Promise((r) => setTimeout(r, 3000));
      const status = await getJobStatus(jobId, accessToken);
      setJob(status);
      if (status.status === 'complete' && status.resultId) {
        setDocumentId(status.resultId);
        setStep(3);
        return;
      }
      if (status.status === 'failed') {
        setError(status.errorMessage ?? 'Article generation failed');
        setStep(1);
        return;
      }
    }
    setError('Generation timed out');
    setStep(1);
  }

  return (
    <main className="mx-auto max-w-2xl p-8">
      <h1 className="text-2xl font-semibold">Guided article</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">ContentShake-style flow — six steps to a scored draft.</p>

      <ol className="mt-6 flex gap-2 text-xs">
        {STEPS.map((label, i) => (
          <li
            key={label}
            className={`rounded-full px-2 py-1 ${i === step ? 'bg-[var(--color-accent)] text-white' : 'bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)]'}`}
          >
            {label}
          </li>
        ))}
      </ol>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      {step === 0 && (
        <div className="mt-8 space-y-4">
          <input
            className="w-full rounded border px-3 py-2"
            placeholder="Business name"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
          <input
            className="w-full rounded border px-3 py-2"
            placeholder="https://yoursite.com"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
          />
          <input
            className="w-full rounded border px-3 py-2"
            placeholder="Location"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
          />
          <button
            type="button"
            className="rounded bg-[var(--color-accent)] px-4 py-2 text-white"
            onClick={() => void onCreateProject()}
          >
            Continue
          </button>
        </div>
      )}

      {step === 1 && (
        <div className="mt-8 space-y-4">
          <input
            className="w-full rounded border px-3 py-2"
            placeholder="Target keyword"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
          />
          <button
            type="button"
            className="text-sm text-[var(--color-text-secondary)] underline"
            disabled={!project || !keyword.trim() || ideasLoading}
            onClick={() =>
              void (async () => {
                if (!project) return;
                setIdeasLoading(true);
                setError(null);
                try {
                  setIdeas(
                    await researchKeywords(
                      { projectId: project.id, seedKeyword: keyword, location, resultCount: 15 },
                      accessToken,
                    ),
                  );
                } catch (e) {
                  setError(e instanceof Error ? e.message : 'Keyword ideas failed');
                } finally {
                  setIdeasLoading(false);
                }
              })()
            }
          >
            {ideasLoading ? 'Loading ideas…' : 'Find keyword ideas'}
          </button>
          {ideas.length > 0 && (
            <ul className="max-h-40 space-y-1 overflow-y-auto rounded border bg-white p-2 text-sm">
              {ideas.map((k) => (
                <li key={k.keyword}>
                  <button
                    type="button"
                    className="w-full rounded px-2 py-1 text-left hover:bg-[var(--color-surface-muted)]"
                    onClick={() => setKeyword(k.keyword)}
                  >
                    {k.keyword}{' '}
                    <span className="text-[var(--color-text-secondary)]">({k.searchVolume.toLocaleString()}/mo)</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
          <button
            type="button"
            className="rounded bg-[var(--color-accent)] px-4 py-2 text-white disabled:opacity-50"
            disabled={!keyword.trim()}
            onClick={() => void onGenerate()}
          >
            Generate article with AI
          </button>
        </div>
      )}

      {step === 2 && (
        <div className="mt-8">
          <p className="text-[var(--color-text-secondary)]">Generating… {job?.progressPercent ?? 0}%</p>
          <div className="mt-4 h-2 w-full overflow-hidden rounded bg-[var(--color-surface-muted)]">
            <div
              className="h-full bg-[var(--color-accent)] transition-all"
              style={{ width: `${job?.progressPercent ?? 10}%` }}
            />
          </div>
        </div>
      )}

      {step === 3 && documentId && (
        <div className="mt-8 space-y-4">
          <p className="text-[var(--color-text-secondary)]">Your draft is ready. Open the editor to review your score.</p>
          <Link
            href={`/app/content/${documentId}`}
            className="inline-block rounded bg-[var(--color-accent)] px-4 py-2 text-white"
          >
            Open editor
          </Link>
          <button type="button" className="ml-3 text-sm underline" onClick={() => setStep(4)}>
            Publish checklist
          </button>
        </div>
      )}

      {step === 4 && (
        <div className="mt-8 space-y-2 text-sm text-[var(--color-text-primary)]">
          <p>Before publishing:</p>
          <ul className="list-inside list-disc space-y-1">
            <li>Content score ≥ 70</li>
            <li>Meta description filled</li>
            <li>Target keyword in title</li>
            <li>Optional: publish to WordPress if connected on project settings</li>
          </ul>
          {documentId && project && (
            <button
              type="button"
              className="mt-4 rounded border px-4 py-2 text-sm hover:bg-[var(--color-surface-muted)]"
              onClick={() =>
                void (async () => {
                  setError(null);
                  try {
                    const result = await publishToWordPress(
                      documentId,
                      { postStatus: 'draft' },
                      accessToken,
                    );
                    setPublishUrl(result.url);
                    setStep(5);
                  } catch (e) {
                    setError(e instanceof Error ? e.message : 'WordPress publish failed — use Skip or copy HTML from the editor');
                  }
                })()
              }
            >
              Publish to WordPress (if connected)
            </button>
          )}
          <button
            type="button"
            className="mt-4 rounded bg-[var(--color-accent)] px-4 py-2 text-sm text-white"
            onClick={() => setStep(5)}
          >
            Done — open editor
          </button>
        </div>
      )}

      {step === 5 && (
        <div className="mt-8">
          <p className="text-[var(--color-text-secondary)]">Great work. Continue in Expert mode anytime.</p>
          {publishUrl && (
            <a href={publishUrl} target="_blank" rel="noreferrer" className="mt-2 block text-sm text-green-700 underline">
              View WordPress draft
            </a>
          )}
          <Link href="/app/projects" className="mt-4 inline-block text-sm underline">
            Back to projects
          </Link>
        </div>
      )}
    </main>
  );
}
