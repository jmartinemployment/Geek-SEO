'use client';

import { useEffect, useState } from 'react';
import type { ContentSequenceItem, InternalLinkingBlueprint, LinkGraphEdge } from '@/lib/seo-api';
import { getLinksBlueprint } from '@/lib/seo-api';

type Props = {
  projectId: string;
  accessToken: string | null;
};

export function LinkingBlueprintTab({ projectId, accessToken }: Props) {
  const [blueprint, setBlueprint] = useState<InternalLinkingBlueprint | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [activeTab, setActiveTab] = useState<'sequence' | 'graph'>('sequence');

  useEffect(() => {
    async function load() {
      if (!projectId) return;
      setLoading(true);
      setError(null);
      try {
        const data = await getLinksBlueprint(projectId, accessToken);
        setBlueprint(data);
      } catch (err) {
        setError(err);
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, [projectId, accessToken]);

  if (loading) return <div className="p-6 text-sm text-[var(--color-text-muted)]">Loading...</div>;
  if (error) return <div className="p-6 text-sm text-red-600">Error loading blueprint</div>;
  if (!blueprint) return <div className="p-6 text-sm text-[var(--color-text-muted)]">No linking blueprint available</div>;

  return (
    <div className="space-y-4">
      {/* Tab switcher */}
      <div className="flex gap-2 border-b">
        <button
          onClick={() => setActiveTab('sequence')}
          className={`px-4 py-2 text-sm font-medium ${
            activeTab === 'sequence'
              ? 'border-b-2 border-[var(--color-brand)] text-[var(--color-brand)]'
              : 'text-[var(--color-text-muted)] hover:text-[var(--color-text)]'
          }`}
        >
          Publish Order ({blueprint.sequences.length})
        </button>
        <button
          onClick={() => setActiveTab('graph')}
          className={`px-4 py-2 text-sm font-medium ${
            activeTab === 'graph'
              ? 'border-b-2 border-[var(--color-brand)] text-[var(--color-brand)]'
              : 'text-[var(--color-text-muted)] hover:text-[var(--color-text)]'
          }`}
        >
          Link Graph ({blueprint.linkGraph.length})
        </button>
      </div>

      {/* Publish Order Tab */}
      {activeTab === 'sequence' && (
        <SequenceView sequences={blueprint.sequences} />
      )}

      {/* Link Graph Tab */}
      {activeTab === 'graph' && (
        <GraphView edges={blueprint.linkGraph} />
      )}
    </div>
  );
}

function SequenceView({ sequences }: { sequences: ContentSequenceItem[] }) {
  return (
    <div className="space-y-2">
      {sequences.length === 0 ? (
        <p className="text-sm text-[var(--color-text-muted)]">No sequence data</p>
      ) : (
        sequences.map((item) => (
          <div
            key={`${item.order}-${item.topicId}`}
            className="flex items-start gap-4 rounded-lg border border-[var(--color-border)] p-3"
          >
            <div className="flex min-w-fit items-center justify-center rounded-full bg-[var(--color-accent)] px-3 py-1 font-mono text-sm font-bold text-white">
              {item.order}
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <h3 className="text-sm font-medium">{item.topicName}</h3>
                <span className="text-xs px-2 py-0.5 rounded-full bg-blue-100 text-blue-700">
                  {item.tier}
                </span>
              </div>
              {item.reason && (
                <p className="text-xs text-[var(--color-text-muted)] mt-1">{item.reason}</p>
              )}
            </div>
          </div>
        ))
      )}
    </div>
  );
}

function GraphView({ edges }: { edges: LinkGraphEdge[] }) {
  // Group edges by source for readability
  const grouped: Record<string, LinkGraphEdge[]> = {};
  edges.forEach((edge) => {
    if (!grouped[edge.sourceTopicId]) grouped[edge.sourceTopicId] = [];
    grouped[edge.sourceTopicId]!.push(edge);
  });

  return (
    <div className="space-y-4">
      {Object.entries(grouped).length === 0 ? (
        <p className="text-sm text-[var(--color-text-muted)]">No link recommendations</p>
      ) : (
        Object.entries(grouped).map(([source, edges_]) => (
          <div key={source} className="space-y-2">
            <div className="text-sm font-medium">{source}</div>
            <div className="ml-4 space-y-1">
              {edges_.map((edge, idx) => (
                <div key={idx} className="flex items-center gap-2 text-xs">
                  <span className="text-[var(--color-text-muted)]">→</span>
                  <span>{edge.targetTopicId}</span>
                  <span className="text-[var(--color-text-muted)]">(anchor: "{edge.anchorText}")</span>
                  <span
                    className={`ml-auto px-2 py-0.5 rounded-full ${
                      edge.priority === 'high'
                        ? 'bg-red-100 text-red-700'
                        : edge.priority === 'medium'
                          ? 'bg-amber-100 text-amber-700'
                          : 'bg-gray-100 text-gray-700'
                    }`}
                  >
                    {edge.priority ?? 'medium'}
                  </span>
                </div>
              ))}
            </div>
          </div>
        ))
      )}
    </div>
  );
}
