'use client';

import { useMemo } from 'react';
import {
  Background,
  Controls,
  MiniMap,
  ReactFlow,
  type Node,
  type NodeProps,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import type { TopicalMapTopic } from '@/lib/seo-api';

type TopicNodeData = {
  topic: TopicalMapTopic;
};

function coverageColor(coverage: TopicalMapTopic['coverage']): string {
  if (coverage === 'covered') return '#dcfce7';
  if (coverage === 'partial') return '#fef3c7';
  if (coverage === 'opportunity') return '#e0e7ff';
  return '#fee2e2';
}

function coverageBorder(coverage: TopicalMapTopic['coverage']): string {
  if (coverage === 'covered') return '#16a34a';
  if (coverage === 'partial') return '#d97706';
  if (coverage === 'opportunity') return '#4f46e5';
  return '#dc2626';
}

function tierBadgeColor(tier?: string): string {
  if (tier === 'Pillar') return '#2563eb bg-blue-100';
  if (tier === 'Cluster') return '#9333ea text-purple-600';
  return '#6b7280 text-gray-600';
}

function TopicNode({ data }: NodeProps<Node<TopicNodeData>>) {
  const { topic } = data;
  const widthClass = topic.tier === 'Pillar' ? 'w-[240px]' : 'w-[220px]';
  const paddingClass = topic.tier === 'Pillar' ? 'py-3' : 'py-2';

  return (
    <div
      className={`${widthClass} rounded-lg border-2 px-3 ${paddingClass} shadow-sm`}
      style={{
        background: coverageColor(topic.coverage),
        borderColor: coverageBorder(topic.coverage),
      }}
    >
      <div className="flex items-start justify-between gap-2">
        <p className="text-xs font-semibold text-[var(--color-text-primary)] line-clamp-2 flex-1">{topic.name}</p>
        {topic.tier ? (
          <span className="shrink-0 rounded-full bg-opacity-20 px-2 py-0.5 text-[10px] font-medium uppercase" style={{
            backgroundColor: topic.tier === 'Pillar' ? '#dbeafe' : topic.tier === 'Cluster' ? '#f3e8ff' : '#f3f4f6',
            color: topic.tier === 'Pillar' ? '#1e40af' : topic.tier === 'Cluster' ? '#6b21a8' : '#4b5563'
          }}>
            {topic.tier}
          </span>
        ) : null}
      </div>
      <p className="mt-1 text-[10px] uppercase tracking-wide text-[var(--color-text-muted)]">
        {topic.coverage}
        {topic.clusterMethod ? ` · ${topic.clusterMethod}` : ''}
      </p>
      <p className="mt-1 text-[10px] text-[var(--color-text-secondary)]">
        {topic.searchVolume ? `${topic.searchVolume.toLocaleString()} vol` : '—'}
        {topic.priorityScore != null ? ` · prio ${topic.priorityScore}` : ''}
      </p>
    </div>
  );
}

const nodeTypes = { topic: TopicNode };

type TopicalMapGraphProps = {
  topics: TopicalMapTopic[];
  selectedName: string | null;
  onSelect: (topic: TopicalMapTopic) => void;
};

export function TopicalMapGraph({ topics, selectedName, onSelect }: TopicalMapGraphProps) {
  const { nodes } = useMemo(() => {
    const byPillar = new Map<string, TopicalMapTopic[]>();
    for (const topic of topics) {
      const pillar = topic.pillarName ?? 'General';
      const list = byPillar.get(pillar) ?? [];
      list.push(topic);
      byPillar.set(pillar, list);
    }

    const pillars = [...byPillar.keys()].sort((a, b) => a.localeCompare(b));
    const flowNodes: Node<TopicNodeData>[] = [];

    pillars.forEach((pillar, col) => {
      const list = [...(byPillar.get(pillar) ?? [])].sort(
        (a, b) => (b.priorityScore ?? 0) - (a.priorityScore ?? 0),
      );
      list.forEach((topic, row) => {
        flowNodes.push({
          id: `${pillar}-${topic.name}-${row}`,
          type: 'topic',
          position: { x: col * 260, y: row * 110 },
          data: { topic },
          selected: selectedName === topic.name,
        });
      });
    });

    return { nodes: flowNodes };
  }, [topics, selectedName]);

  if (topics.length === 0) {
    return (
      <p className="rounded-xl border border-dashed bg-[var(--color-surface)] p-8 text-center text-sm text-[var(--color-text-secondary)]">
        No topics to visualize. Connect GSC and generate a map.
      </p>
    );
  }

  return (
    <div className="h-[520px] rounded-xl border bg-white overflow-hidden">
      <ReactFlow
        nodes={nodes}
        edges={[]}
        nodeTypes={nodeTypes}
        fitView
        onNodeClick={(_, node) => onSelect(node.data.topic)}
        proOptions={{ hideAttribution: true }}
      >
        <Background gap={16} size={1} />
        <Controls />
        <MiniMap pannable zoomable />
      </ReactFlow>
    </div>
  );
}
