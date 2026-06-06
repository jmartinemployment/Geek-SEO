import type { PillarUrlMatch } from '@/lib/niche-url-match';

type Props = {
  match: PillarUrlMatch;
};

const coverageClass: Record<PillarUrlMatch['coverageStatus'], string> = {
  gap: 'border-amber-200 bg-amber-50 text-amber-950',
  partial: 'border-sky-200 bg-sky-50 text-sky-950',
  covered: 'border-emerald-200 bg-emerald-50 text-emerald-950',
};

export function ContentGuardPillarBadge({ match }: Readonly<Props>) {
  return (
    <span
      className={`inline-flex max-w-full items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium ${coverageClass[match.coverageStatus]}`}
      title={`Matched niche pillar (${match.matchKind.replace('_', ' ')})`}
    >
      <span className="truncate">{match.pillarTopic}</span>
      <span className="shrink-0 opacity-70">· {match.coverageStatus}</span>
    </span>
  );
}
