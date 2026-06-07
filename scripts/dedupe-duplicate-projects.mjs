#!/usr/bin/env node
/**
 * Removes duplicate SEO projects that share the same normalized site URL.
 * Keeps one project per domain (prefers GSC-connected, then newest UpdatedAt).
 *
 * Usage:
 *   node scripts/dedupe-duplicate-projects.mjs
 *   DRY_RUN=1 node scripts/dedupe-duplicate-projects.mjs
 *   TARGET_DOMAIN=https://www.geekatyourspot.com node scripts/dedupe-duplicate-projects.mjs
 *
 * Env:
 *   PLAYWRIGHT_API_URL or NEXT_PUBLIC_SEO_API_URL — API base (default production)
 *   INTEGRATION_USER_ID — GeekOAuth user id (default production operator id)
 */
const apiBase = (
  process.env.PLAYWRIGHT_API_URL ??
  process.env.NEXT_PUBLIC_SEO_API_URL ??
  'https://seo-api.geekatyourspot.com'
).replace(/\/$/u, '');

const userId =
  process.env.INTEGRATION_USER_ID ?? '92b274f5-2fcb-4935-ba2d-cd8c03e1b21b';

const targetDomain = (
  process.env.TARGET_DOMAIN ?? 'https://www.geekatyourspot.com'
).replace(/\/$/u, '');

const dryRun = process.env.DRY_RUN === '1' || process.env.DRY_RUN === 'true';

const headers = {
  'X-User-Id': userId,
  Accept: 'application/json',
  'Content-Type': 'application/json',
};

function normalizeSiteUrl(raw) {
  const value = raw.trim();
  const withScheme = value.startsWith('http') ? value : `https://${value}`;
  const url = new URL(withScheme);
  return `${url.protocol}//${url.host}`.toLowerCase();
}

function pickKeeper(projects) {
  return [...projects].sort((a, b) => {
    if (a.gscConnected !== b.gscConnected) return a.gscConnected ? -1 : 1;
    const aTime = Date.parse(a.updatedAt ?? a.createdAt ?? '') || 0;
    const bTime = Date.parse(b.updatedAt ?? b.createdAt ?? '') || 0;
    if (aTime !== bTime) return bTime - aTime;
    return a.name.localeCompare(b.name);
  })[0];
}

async function main() {
  const listRes = await fetch(`${apiBase}/api/seo/projects`, { headers });
  if (!listRes.ok) {
    throw new Error(`list projects failed: ${listRes.status} ${await listRes.text()}`);
  }

  const projects = await listRes.json();
  const normalizedTarget = normalizeSiteUrl(targetDomain);
  const dupes = projects.filter(
    (p) => normalizeSiteUrl(p.url) === normalizedTarget,
  );

  if (dupes.length <= 1) {
    console.log(`No duplicates for ${normalizedTarget} (${dupes.length} project).`);
    return;
  }

  const keep = pickKeeper(dupes);
  const remove = dupes.filter((p) => p.id !== keep.id);

  console.log(`Domain: ${normalizedTarget}`);
  console.log(`Keep: ${keep.name} (${keep.id}) gsc=${keep.gscConnected}`);
  console.log(`Remove ${remove.length} duplicate(s):`);
  for (const p of remove) {
    console.log(`  - ${p.name} (${p.id}) gsc=${p.gscConnected}`);
  }

  if (dryRun) {
    console.log('DRY_RUN=1 — no deletes performed.');
    return;
  }

  for (const p of remove) {
    const delRes = await fetch(`${apiBase}/api/seo/projects/${p.id}`, {
      method: 'DELETE',
      headers,
    });
    if (!delRes.ok) {
      throw new Error(`delete ${p.id} failed: ${delRes.status} ${await delRes.text()}`);
    }
    console.log(`Deleted ${p.name} (${p.id})`);
  }

  console.log('Done.');
}

main().catch((err) => {
  console.error(err.message ?? err);
  process.exit(1);
});
