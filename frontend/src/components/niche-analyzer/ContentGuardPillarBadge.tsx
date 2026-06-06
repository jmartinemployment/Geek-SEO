import { pillarCoverageLabel, type PillarUrlMatch } from '@/lib/niche-url-match';

type Props = {
  match: PillarUrlMatch;
};

const coverageClass: Record<PillarUrlMatch['coverageStatus'], string> = {
  gap: 'border-amber-200 bg-amber-50 text-amber-950',
  partial: 'border-sky-200 bg-sky-50 text-sky-950',
  covered: 'border-emerald-200 bg-emerald-50 text-emerald-950',
};

export function ContentGuardPillarBadge({ match }: Readonly<Props>) {
  const hint = pillarCoverageLabel(match.coverageStatus);

  return (
    <div
      className={`inline-flex max-w-full flex-col gap-0.5 rounded-lg border px-2.5 py-1.5 text-xs ${coverageClass[match.coverageStatus]}`}
    >
      <p className="font-medium leading-snug">
        Topic from niche analysis: <span className="font-semibold">{match.pillarTopic}</span>
      </p>
      <p className="leading-snug opacity-90">{hint}</p>
    </div>
  );
}
