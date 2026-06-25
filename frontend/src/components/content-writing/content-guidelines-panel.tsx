'use client';

import { useMemo } from 'react';
import { parseContentWriterKeywordBundle } from '@/lib/seo-api';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';

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
    const fromRecommendations = bundle?.writingRecommendations ?? [];
    const fromHeadings =
      bundle?.sourceHeadings
        ?.map((h) => h.text.trim())
        .filter((t) => t.length > 2 && t.split(/\s+/).length <= 4) ?? [];
    const unique = new Set<string>();
    for (const term of [...fromRecommendations, ...fromHeadings, keyword]) {
      const trimmed = term.trim();
      if (trimmed) unique.add(trimmed);
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
