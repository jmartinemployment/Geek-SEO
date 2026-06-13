'use client';

import { Fragment, useState } from 'react';
import type { NicheCompetitorResult } from '@/lib/seo-api';

type Props = {
  competitors: NicheCompetitorResult[];
};

const STRENGTH_COLORS: Record<string, string> = {
  dominant: 'bg-rose-100 text-rose-800',
  strong: 'bg-amber-100 text-amber-800',
  moderate: 'bg-stone-100 text-stone-600',
};

const SCOPE_COLORS: Record<string, string> = {
  both: 'bg-violet-100 text-violet-800',
  national: 'bg-blue-100 text-blue-800',
  local: 'bg-emerald-100 text-emerald-800',
};

export function NicheCompetitorPanel({ competitors }: Readonly<Props>) {
  const [expandedDomain, setExpandedDomain] = useState<string | null>(null);

  if (competitors.length === 0) {
    return (
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-8 text-center">
        <p className="text-sm text-[var(--color-text-muted)]">No competitors identified. Run analysis to populate.</p>
      </div>
    );
  }

  const totalPages = competitors.reduce((sum, c) => sum + (c.pagesCrawled ?? 0), 0);

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="border-b border-[var(--color-border)] px-5 py-4">
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">Competitor landscape</h3>
        <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
          {competitors.length} competitor{competitors.length !== 1 ? 's' : ''} identified via SERP footprint
          {totalPages > 0 ? ` · ${totalPages} pages crawled` : ''}
        </p>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full text-left text-xs">
          <thead className="bg-[var(--color-surface-muted)] text-[var(--color-text-muted)]">
            <tr>
              <th className="w-8 px-2 py-2" aria-label="Expand" />
              <th className="px-3 py-2 font-medium">Domain</th>
              <th className="px-3 py-2 font-medium">Scope</th>
              <th className="px-3 py-2 font-medium">Strength</th>
              <th className="px-3 py-2 font-medium">SERP presence</th>
              <th className="px-3 py-2 font-medium">Pillars ranking</th>
              <th className="px-3 py-2 font-medium">Pages crawled</th>
              <th className="px-3 py-2 font-medium">Avg words</th>
            </tr>
          </thead>
          <tbody>
            {competitors.map((c) => {
              const expanded = expandedDomain === c.domain;
              const hasCrawlData = c.pagesCrawled > 0;
              return (
                <Fragment key={c.domain}>
                  <tr className="border-t border-[var(--color-border)]">
                    <td className="px-2 py-2">
                      {hasCrawlData ? (
                        <button
                          type="button"
                          aria-expanded={expanded}
                          onClick={() => setExpandedDomain(expanded ? null : c.domain)}
                          className="flex h-6 w-6 items-center justify-center rounded text-[var(--color-text-muted)] hover:bg-[var(--color-surface-muted)]"
                        >
                          {expanded ? '−' : '+'}
                        </button>
                      ) : null}
                    </td>
                    <td className="px-3 py-2 font-medium text-[var(--color-text-primary)]">{c.domain}</td>
                    <td className="px-3 py-2">
                      <span className={`rounded px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide ${SCOPE_COLORS[c.scope] ?? 'bg-stone-100 text-stone-600'}`}>
                        {c.scope}
                      </span>
                    </td>
                    <td className="px-3 py-2">
                      <span className={`rounded px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide ${STRENGTH_COLORS[c.strengthAssessment] ?? 'bg-stone-100 text-stone-600'}`}>
                        {c.strengthAssessment}
                      </span>
                    </td>
                    <td className="px-3 py-2 tabular-nums text-[var(--color-text-secondary)]">{c.serpPresence}</td>
                    <td className="px-3 py-2 tabular-nums text-[var(--color-text-secondary)]">{c.pillarsRanking}</td>
                    <td className="px-3 py-2 tabular-nums text-[var(--color-text-secondary)]">{hasCrawlData ? c.pagesCrawled : '—'}</td>
                    <td className="px-3 py-2 tabular-nums text-[var(--color-text-secondary)]">{hasCrawlData ? c.avgWordCount.toLocaleString() : '—'}</td>
                  </tr>
                  {expanded && hasCrawlData ? (
                    <tr key={`${c.domain}-detail`} className="border-t border-[var(--color-border)] bg-[var(--color-surface-muted)]/40">
                      <td colSpan={8} className="px-5 py-4">
                        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                          {c.description ? (
                            <div className="col-span-full">
                              <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">Description</p>
                              <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{c.description}</p>
                            </div>
                          ) : null}
                          {(c.services?.length ?? 0) > 0 ? (
                            <div>
                              <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">Services ({c.services!.length})</p>
                              <ul className="mt-1 space-y-0.5">
                                {c.services!.slice(0, 10).map((s) => <li key={s} className="text-xs text-[var(--color-text-secondary)]">{s}</li>)}
                                {c.services!.length > 10 ? <li className="text-[10px] text-[var(--color-text-muted)]">+{c.services!.length - 10} more</li> : null}
                              </ul>
                            </div>
                          ) : null}
                          {(c.knowsAbout?.length ?? 0) > 0 ? (
                            <div>
                              <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">Knows about ({c.knowsAbout!.length})</p>
                              <ul className="mt-1 space-y-0.5">
                                {c.knowsAbout!.slice(0, 10).map((k) => <li key={k} className="text-xs text-[var(--color-text-secondary)]">{k}</li>)}
                                {c.knowsAbout!.length > 10 ? <li className="text-[10px] text-[var(--color-text-muted)]">+{c.knowsAbout!.length - 10} more</li> : null}
                              </ul>
                            </div>
                          ) : null}
                          {(c.areaServed?.length ?? 0) > 0 ? (
                            <div>
                              <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">Area served ({c.areaServed!.length})</p>
                              <ul className="mt-1 space-y-0.5">
                                {c.areaServed!.slice(0, 8).map((a) => <li key={a} className="text-xs text-[var(--color-text-secondary)]">{a}</li>)}
                                {c.areaServed!.length > 8 ? <li className="text-[10px] text-[var(--color-text-muted)]">+{c.areaServed!.length - 8} more</li> : null}
                              </ul>
                            </div>
                          ) : null}
                          {c.hasFaqSchema ? (
                            <div className="flex items-start">
                              <span className="rounded bg-blue-50 px-2 py-0.5 text-[10px] font-medium text-blue-700">FAQ schema</span>
                            </div>
                          ) : null}
                        </div>
                      </td>
                    </tr>
                  ) : null}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}
