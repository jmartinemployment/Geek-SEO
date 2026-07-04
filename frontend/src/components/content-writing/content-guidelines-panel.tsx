'use client';

import { useMemo } from 'react';
import { parseContentWriterKeywordBundle } from '@/lib/seo-api';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';

const FOUR_PHASE_METHODOLOGY = [
  {
    label: 'Business Objectives',
    intent:
      'Define why this initiative matters now: target outcomes, stakeholders, success metrics, and the business case or ROI.',
  },
  {
    label: 'Data Quality Assessment',
    intent:
      'Evaluate data readiness for AI and automation — source quality, gaps, cleanup work, and prerequisites before tools run reliably.',
  },
  {
    label: 'Choose the Right AI Technologies',
    intent:
      'Compare AI models, platforms, agents, and integrations for this use case, including build-vs-buy, vendor fit, and security.',
  },
  {
    label: 'Implementation Strategy',
    intent:
      'Lay out a practical rollout: pilot scope, timeline, milestones, proof of value, team adoption, and first-phase success metrics.',
  },
] as const;

function plainTextFromHtml(html: string): string {
  return html
    .replace(/<script[\s\S]*?<\/script>/gi, ' ')
    .replace(/<style[\s\S]*?<\/style>/gi, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

function countWords(text: string): number {
  if (!text.trim()) return 0;
  return text.split(/\s+/).filter(Boolean).length;
}

function countTermOccurrences(text: string, term: string): number {
  const normalized = text.toLowerCase();
  const needle = term.toLowerCase().trim();
  if (!needle) return 0;
  let count = 0;
  let index = 0;
  while (index < normalized.length) {
    const found = normalized.indexOf(needle, index);
    if (found === -1) break;
    count += 1;
    index = found + needle.length;
  }
  return count;
}

export function ContentGuidelinesPanel({ keyword }: { keyword: string }) {
  const { doc, html } = useWritingWorkspace();
  const bundle = useMemo(
    () => parseContentWriterKeywordBundle(doc.keywordBundleJson),
    [doc.keywordBundleJson],
  );

  const plainText = useMemo(() => plainTextFromHtml(html), [html]);
  const wordCount = useMemo(() => countWords(plainText), [plainText]);
  const targetWords = bundle?.benchmarks?.medianWordCountTop5 ?? null;
  const targetH2 = bundle?.benchmarks?.medianH2CountTop5 ?? null;

  const terms = useMemo(() => {
    const fromHeadings =
      bundle?.sourceHeadings
        ?.map((h) => h.text.trim())
        .filter((t) => t.length > 2 && t.split(/\s+/).length <= 6) ?? [];
    const fromGaps = bundle?.gapTopics?.map((t) => t.trim()).filter(Boolean) ?? [];
    const unique = new Set<string>();
    const normalizedKeyword = keyword.trim();
    if (normalizedKeyword) unique.add(normalizedKeyword);
    for (const term of [...fromGaps, ...fromHeadings]) {
      if (term.includes(':') || term.toLowerCase().startsWith('keyword ')) continue;
      unique.add(term);
    }
    return [...unique].slice(0, 24);
  }, [bundle, keyword]);

  const termRows = useMemo(
    () =>
      terms.map((term) => ({
        term,
        count: countTermOccurrences(plainText, term),
      })),
    [plainText, terms],
  );

  if (!bundle) {
    return (
      <div className="border-t px-3 py-4 text-xs text-[var(--color-text-secondary)] xl:px-4">
        Guidelines appear when frozen keyword research is attached.
      </div>
    );
  }

  const wordPct =
    targetWords && targetWords > 0
      ? Math.min(100, Math.round((wordCount / targetWords) * 100))
      : null;

  return (
    <div className="space-y-3 border-t px-3 py-4 xl:px-4">
      <h3 className="text-xs font-semibold xl:text-sm">Guidelines</h3>

      <div className="rounded-lg border bg-[var(--color-surface-muted)]/30 p-3">
        <div className="flex items-baseline justify-between gap-2 text-xs">
          <span className="font-medium text-[var(--color-text-primary)]">Word count</span>
          <span className="tabular-nums text-[var(--color-text-secondary)]">
            {wordCount.toLocaleString()}
            {targetWords ? ` / ${targetWords.toLocaleString()}` : ''}
          </span>
        </div>
        {wordPct !== null ? (
          <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-[var(--color-surface-muted)]">
            <div
              className="h-full rounded-full bg-[var(--color-accent)] transition-all duration-300"
              style={{ width: `${wordPct}%` }}
            />
          </div>
        ) : null}
        {targetH2 ? (
          <p className="mt-2 text-[10px] text-[var(--color-text-muted)]">
            Target ~{targetH2} H2 sections (from top-ranking pages)
          </p>
        ) : null}
      </div>

      <div>
        <h4 className="text-[10px] font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
          Four Phase Methodology
        </h4>
        <ol className="mt-2 space-y-2 text-xs">
          {FOUR_PHASE_METHODOLOGY.map((phase, index) => (
            <li key={phase.label} className="rounded-md border border-transparent px-1 py-0.5">
              <span className="font-medium text-[var(--color-text-primary)]">
                {index + 1}. {phase.label}
              </span>
              <p className="mt-0.5 text-[10px] leading-snug text-[var(--color-text-muted)]">{phase.intent}</p>
            </li>
          ))}
        </ol>
      </div>

      {termRows.length ? (
        <div>
          <h4 className="text-[10px] font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
            Terms to use
          </h4>
          <ul className="mt-2 max-h-56 space-y-1 overflow-y-auto text-xs">
            {termRows.map(({ term, count }) => (
              <li
                key={term}
                className="flex items-center justify-between gap-2 rounded-md border border-transparent px-1 py-0.5 hover:border-[var(--color-border)]"
              >
                <span className={count > 0 ? 'text-[var(--color-text-primary)]' : 'text-[var(--color-text-muted)]'}>
                  {term}
                </span>
                <span
                  className={`tabular-nums text-[10px] font-medium ${
                    count > 0 ? 'text-emerald-700' : 'text-amber-700'
                  }`}
                >
                  {count}
                </span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}
