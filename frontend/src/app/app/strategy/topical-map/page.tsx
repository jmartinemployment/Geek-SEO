'use client';

import { useEffect, useState } from 'react';
import { TopicalMapWorkspace } from '@/components/strategy/topical-map-workspace';
import { useAuthReady } from '@/hooks/use-auth-ready';
import { listProjects, type SeoProject } from '@/lib/seo-api';

export default function TopicalMapPage() {
  const { accessToken, authLoading, authReady } = useAuthReady();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');

  useEffect(() => {
    if (!authReady) return;
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
  }, [accessToken, authReady]);

  const selected = projects.find((p) => p.id === projectId);

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-7xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">Topical map</h1>
      <p className="mt-1 max-w-3xl text-sm text-[var(--color-text-secondary)]">
        GSC query clusters by landing page and SERP overlap — prioritized gaps, pillars, and competitor domains.
        Professional tier + connected Search Console required.
      </p>

      <div className="mt-6">
        <label className="text-sm font-medium">
          Project
          <select
            className="ml-2 rounded-lg border px-3 py-2"
            value={projectId}
            onChange={(e) => setProjectId(e.target.value)}
          >
            {projects.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      {projectId && selected ? (
        <div className="mt-8">
          <TopicalMapWorkspace
            projectId={projectId}
            projectName={selected.name}
            accessToken={accessToken}
          />
        </div>
      ) : null}
    </main>
  );
}
