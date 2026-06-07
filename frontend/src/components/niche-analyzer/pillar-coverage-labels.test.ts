import { describe, expect, it } from 'vitest';
import { countPillarCoverage, groupPillarsByCoverage } from '@/components/niche-analyzer/pillar-coverage-labels';

describe('groupPillarsByCoverage', () => {
  it('partitions pillars into covered, partial, and gap buckets', () => {
    const rows = [
      { id: '1', coverageStatus: 'covered', pillarTopic: 'A' },
      { id: '2', coverageStatus: 'partial', pillarTopic: 'B' },
      { id: '3', coverageStatus: 'gap', pillarTopic: 'C' },
      { id: '4', coverageStatus: 'partial', pillarTopic: 'D' },
    ];

    const grouped = groupPillarsByCoverage(rows);

    expect(grouped.covered).toHaveLength(1);
    expect(grouped.partial).toHaveLength(2);
    expect(grouped.gap).toHaveLength(1);
    expect(countPillarCoverage(rows)).toEqual({ covered: 1, partial: 2, gap: 1 });
  });
});
