# Session Handoff — June 29, 2026

## What shipped today

### Content Writer infrastructure (geekatyourspot-r repo)

**Dual-summary data model** — `summary` field split into three required, distinct, surface-specific fields on `UseCasePage`:
- `homeSummary` — homepage use-cases section (punchy, outcome-driven)
- `hubSummary` — hub card + JSON-LD schema (definitional, snippet-shaped)
- `metaDescription` — page `<meta>` (150–160 chars, distinct wording)
- `primaryKeyword` — feeds build-time keyword-registry collision gate

All 4 department catalogs (marketing, accounting, customer-service, human-resources) rewritten with all new fields. Existing `summary` field removed.

**Build gates** (run at every `next build`):
- `lib/content/validate.ts` — required-distinct gate + FAQ duplication check
- `lib/seo/keyword-registry.ts` — exact/substring collision check across all use-case and blog `primaryKeyword` values
- `next.config.ts` calls `validateContent()` at build time

**Blog infrastructure** — types, catalog, static registry (for FAQ gate), reverse-index (`useCaseSlug → BlogPost[]`), App Router pages (`/blog`, `/blog/[slug]`), BlogCard component, BlogPosting JSON-LD, sitemap integration. Pillar pages show "Related reading" block from reverse index.

**Social infrastructure** — `SocialPost` type with optional `linkTargets[]` (defaults to pillar + all related blog spokes), `validate-social.ts` CI lint (incompleteness, platform collision, dangling slugs). Separate from site build.

**Content output directory** — `/Users/jeffmartin/Documents/Geek-SEO/content-output/` — Content Writer stores deliverables here as flat files. Jeff copies manually to geekatyourspot-r and posts social.

**Writer brief** — `docs/content-writer-brief.md` in geekatyourspot-r — covers all 7 assets, authoring order (pillar-first), ownership table, build gates vs. social lint, paste targets.

**geekatyourspot-r commits:** `4ec46a5`, `786ae71`

---

### Site Analyzer migration (Geek-SEO repo)

**SA2 workspace moved** from `site-analyzer.geekatyourspot.com` (standalone) into `seo.geekatyourspot.com`:
- `seo.geekatyourspot.com/site-analyzer` — SA2 workspace (primary URL)
- `seo.geekatyourspot.com/strategy/site-analyzer` — same component

**`/app/` URL segment removed** — all routes moved from `/app/*` to `/*`:
- `/app/dashboard` → `/dashboard`
- `/app/strategy/site-analyzer` → `/strategy/site-analyzer`
- etc. (all 17 route directories moved)
- `usesAppShell`, `isProtectedAppRoute`, and proxy middleware matcher updated to enumerate routes explicitly

**SA2 source committed** — `SiteAnalyzer2/` directory (193 files) added to repo so Railway can build it.

**Favicon** — brand GIF (`public/favicon.gif`) now served via `metadata.icons`; old default `favicon.ico` removed.

**Commits:** `66033a6`, `6677996`, `43d3981`, `0e440d1`

---

## Pending: SA2 backend not active on Railway

**Problem:** GeekSeoBackend deployed the SA2 commit (`0e440d1`) in 21 seconds via full cache hit. The cached binary predates the SA2 source commit — SA2 code was not compiled in. `SITE_ANALYZER2_DATABASE_URL` IS set correctly on Railway.

**Symptom:** `GET /api/seo/sa2/sites` returns 404. No "Site Analyzer 2 (sa2) migrations applied." log on startup.

**Fix:** Railway dashboard → GeekSeoBackend → Deployments → Redeploy → **check "Clear Build Cache"**. Forces full recompile with SA2 source. Build will take 10–15 minutes (Playwright install included).

---

## Pending: favicon GIF not rendering in browser

GIF is not a supported favicon format in Chrome/Firefox/Safari. After the Railway issue above is resolved, convert `public/favicon.gif` to PNG:

```bash
sips -s format png frontend/public/favicon.gif --out frontend/public/favicon.png
```

Then update `frontend/src/app/layout.tsx`:
```ts
icons: { icon: '/favicon.png' },
```

Commit and push.

---

## URL cleanup deferred

`/strategy/site-analyzer` is the correct URL pattern. The future cleanup task (logged in `plan-documents/TODO.md`) is to rename `frontend/src/app/app/` → route group pattern so all tool URLs lose the `app/` segment. Already done today — future work is to tidy any remaining `/app/` references if found.

---

## Next steps (priority order)

1. **Redeploy GeekSeoBackend with cleared cache** → verify `seo.geekatyourspot.com/site-analyzer` Create Site Profile works
2. **Convert favicon.gif → favicon.png** → commit/push → verify in browser
3. **Content Writer pilot** — `marketing/content-operations` pillar-first (see `docs/content-writer-brief.md`)
4. Remaining uncommitted backend/backend changes from session start — review and commit separately

---

## Session handoff — July 1, 2026 (manual five-lane research)

### Current direction

- **`sa2` schema** remains the system of record for research — GeekSeoBackend → GeekAPI → GeekRepository.
- **Manual five-lane import** (keyword + edu/gov/local/wiki) is the customer-journey pilot.
- **`research_mode = manual`** — relaxed write gate (`ValidateManualResearchExport`). Full SA2 crawl keeps strict `ResearchBackedWriteGate`.
- **No production-quality DB data** — drop duplicate `geek_seo` research tables; no row migration for pilot.
- **Canonical docs:** [`docs/site-analyzer/MANUAL-FIVE-LANE-RESEARCH.md`](docs/site-analyzer/MANUAL-FIVE-LANE-RESEARCH.md), [ADR 016](docs/site-analyzer/decisions/016-manual-five-lane-research.md).

### Build locations

| Feature | Where |
|---------|--------|
| Lane import API + `sa2.serp_items.research_lane` | GeekBackend (GeekRepository + GeekAPI) |
| `ValidateManualResearchExport`, merger, enricher | `GeekSeo.Application` |
| Lane UI + CLI | Geek-SEO frontend + `scripts/import-serp-html.sh` |

### Uncommitted WIP

Review `GeekSeo.Application` operator research / voice pack changes separately from `SiteAnalyzer2/` lane WIP. Lane persistence goes through GeekAPI, not in-process SA2 DB or `geek_seo` tables.

### Manual research assets

```text
research/{topic_slug}/{lane}/*.html   e.g. research/customer-journey/
```

Import required before draft — disk folders are not read at write time.

### Obsolete (June 29)

“SA2 retired / discard all SiteAnalyzer2 / remove `SITE_ANALYZER2_DATABASE_URL`” — **do not follow** for research. In-repo `SiteAnalyzer2/` may be trimmed; **`sa2` via GeekAPI stays active.**

