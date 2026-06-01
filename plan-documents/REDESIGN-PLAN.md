# Geek SEO — Redesign Plan
# Style + UX Overhaul to Match Semrush Aesthetic

**Status:** Ready for execution (v1.3 — critique incorporated)  
**Date:** 2026-05-29  
**Reference:** Semrush home screenshot (semrush.com/home)

### Shipped in Geek-SEO (May 2026)

Partial progress against this plan — see `PROJECT_STATUS.md` for live status.

| Plan item | Shipped | Remaining |
|-----------|---------|-----------|
| Phase 1 design tokens + app shell | ✅ | Full dark polish |
| Phase 6 site audit (core) | ✅ `/app/audit`, `/app/audit/[projectId]`, Playwright crawl, Professional tier, dashboard SEO/health columns, header URL → audit | Lighthouse-specific health score (uses crawl overall score today) |
| Editor: AI toolbar + internal links + plagiarism | ✅ | GPTZero badge (#6) branding only |
| `/app/content` flat list | ✅ filter by project | — |
| Dashboard overview API | ✅ `GET /api/seo/dashboard/overview` | Persisted site metrics / sparklines (sibling repo) |
| Deep SERP UI | ✅ `/app/serp` + CSV export | Term matrix heatmap (#13 full parity) |
| GSC strategy surfaces | ✅ topical map, cannibalization, content-guard, geo (on-demand) | Scheduled workers + DB persist |
| `/app/audit` → dashboard redirect | ❌ removed | — |
| Auth unit tests | ✅ Vitest + `GeekSeoBackend.Tests` | CI `unit-tests.yml` |

---

## 0. External Prerequisites (blocked until live)

These are **sibling-repo dependencies** — out of scope for Geek-SEO per [BOUNDARIES.md](../BOUNDARIES.md), but hard gates before the listed phases can ship.

| Prerequisite | Repo | Required before | Notes |
|--------------|------|-----------------|-------|
| GeekAPI internal routes (§8.5) | GeekAPI | Phase 4, Phase 6 | Topical map, audits, dashboard overview persistence |
| EF Core migrations (§8.2) | GeekRepository | Phase 4, Phase 6 | `seo_topical_maps`, `seo_site_audits` on `SeoDbContext` |
| `ScoreUpdate` + `nlpTerms[]` contract | **GeekSeo.Application** (this repo after PLATFORM-DECOUPLING M2) | Phase 7 | `ContentScoringService` moves in-repo; transitional: GeekBackend GeekApplication until M2 |
| DataForSEO plan covers backlink/traffic APIs | — | Phase 2b-full | Confirm before shipping public DataForSEO-heavy tools |
| `GOOGLE_PSI_API_KEY` on Railway | — | Phase 2 | Without key: 400 PSI req/day — production will exhaust immediately |

**Approval gate — do not start Phase 4 until:** GeekAPI topical-map internal routes are deployed and smoke-tested; GeekRepository migrations applied.

---

## 1. Design System

### 1.1 Color Tokens

| Token | Value | Usage |
|-------|-------|-------|
| `--color-bg` | `#FFFFFF` | Page / sidebar background |
| `--color-surface` | `#F7F7F8` | Card backgrounds, input fills |
| `--color-border` | `#E8E8EA` | Card borders, dividers |
| `--color-border-strong` | `#C8C8CC` | Active states, focused inputs |
| `--color-text-primary` | `#1A1A2C` | Body text, labels |
| `--color-text-secondary` | `#6B6B80` | Captions, secondary labels |
| `--color-text-muted` | `#A0A0B0` | Placeholder, disabled |
| `--color-accent` | `#3BB37A` | Primary CTA button (Semrush green) |
| `--color-accent-hover` | `#2E9A68` | CTA hover |
| `--color-metric-blue` | `#1A6EBF` | Bold metric numbers (traffic, keywords) |
| `--color-good` | `#22C55E` | Healthy scores, positive deltas |
| `--color-warn` | `#F59E0B` | Warnings, medium scores |
| `--color-bad` | `#EF4444` | Issues, low scores |
| `--color-badge-purple` | `#7C3AED` | "For you" / featured badge |

**Dark mode (v1):** Ship Semrush light theme first. Add `@media (prefers-color-scheme: dark)` token overrides in `globals.css` in Phase 1 — invert bg/surface/text, keep accent green. Full dark polish deferred; do not leave the current broken Geist dark mode in place.

**Tailwind v4:** This project has no `tailwind.config.ts`. Map all tokens via CSS custom properties in `globals.css` and extend `@theme inline` there (not a separate config file).

### 1.2 Module Icon Colors (Feature Cards)

Each feature module gets a fixed icon background color matching Semrush's card style:

| Module | Icon BG | Icon Color |
|--------|---------|------------|
| Content Optimizer | `#E0F2FE` | `#0369A1` (blue) |
| Topical Map | `#F0FDF4` | `#16A34A` (green) |
| Keyword Research | `#FDF4FF` | `#9333EA` (purple) |
| Rankings | `#FFF7ED` | `#EA580C` (orange) |
| Site Audit | `#FFF1F2` | `#E11D48` (red) |
| Analytics | `#F0F9FF` | `#0284C7` (sky) |
| Competitors | `#FEFCE8` | `#CA8A04` (yellow) |

### 1.3 Typography

- **Font**: `Inter` (Google Fonts) — replace current Geist Sans
- **Headings**: `font-weight: 600`, `letter-spacing: -0.02em`
- **Body**: `font-weight: 400`, `font-size: 14px`, `line-height: 1.5`
- **Metric values**: `font-weight: 700`, `color: var(--color-metric-blue)`
- **Card titles**: `font-size: 15px`, `font-weight: 600`

### 1.4 Component Specs

**Cards**
- `border-radius: 12px`
- `border: 1px solid var(--color-border)`
- `box-shadow: 0 1px 3px rgba(0,0,0,0.06)`
- `padding: 16px 20px`
- Hover: `border-color: var(--color-border-strong)`, `box-shadow: 0 2px 8px rgba(0,0,0,0.10)`

**Primary Button (CTA)**
- Background: `var(--color-accent)`
- Color: white
- `border-radius: 8px`, `padding: 8px 20px`, `font-weight: 600`
- Hover: `var(--color-accent-hover)`

**Search Bar (header)**
- Width: ~520px centered in header (desktop); full-width stacked below logo on mobile `<768px`
- `border-radius: 24px`
- `border: 1.5px solid var(--color-border-strong)`
- Search icon left, submit button right (gray magnifying glass)
- Placeholder: "Enter your website or keyword"
- Focus: `border-color: var(--color-accent)`, `box-shadow: 0 0 0 3px rgba(59,179,122,0.15)`

**Score Gauge** (replaces current ring SVG)
- Keep SVG ring, update stroke color to `var(--color-accent)` for 70+, `var(--color-warn)` for 40–69, `var(--color-bad)` for <40
- Center number: `var(--color-metric-blue)`, `font-weight: 700`, `font-size: 28px`

**Metric Row** (domain overview cards)
- Label: `font-size: 11px`, `text-transform: uppercase`, `letter-spacing: 0.08em`, `color: var(--color-text-secondary)`
- Value: `font-size: 22px`, `font-weight: 700`, `color: var(--color-metric-blue)`
- Delta: `font-size: 12px`, green/red depending on direction
- Unavailable data: show `—` (em dash), not fake numbers

**Responsive minimum (Phase 1)**
- Sidebar collapses to bottom tab bar on mobile, or hamburger overlay — icon-only rail is desktop-only
- Header search stacks; feature card row scrolls horizontally

---

## 2. App Shell

### 2.1 Layout Groups

Three distinct chrome layouts — do not apply app sidebar to all routes:

| Route group | Layout | Sidebar |
|-------------|--------|---------|
| `/`, `/pricing`, `/auth/*` | Marketing layout (header only) | None |
| `/tools/*` | Tools layout (header + footer CTA) | None |
| `/app/*` | App layout (icon sidebar + header) | 56px icon rail |

Implement via Next.js route groups: `frontend/src/app/(marketing)/`, `frontend/src/app/(tools)/`, `frontend/src/app/app/`.

### 2.2 Left Sidebar (icon-only)

Replace current top nav bar with a **thin left sidebar** (56px wide) matching Semrush's icon rail. Applies to `/app/*` only.

**Icons (top to bottom):**
1. Home / Dashboard → `/app/dashboard`
2. Topical Map → `/app/topical-map/[lastProjectId]` or project picker if none
3. Content Documents → `/app/content`
4. Keyword Research → `/app/keywords`
5. Rankings → `/app/rankings`
6. Site Audit → `/app/audit/[lastProjectId]` or project picker
7. Analytics → `/app/analytics`
8. More (⋯) → overflow menu: Guided flow, Planner, Brand voice, Briefs, Calendar (placeholder), GEO (placeholder), Content guard (placeholder)

**Removed from primary sidebar:** Competitors — competitors are document-scoped (`/api/seo/content/{id}/competitors`), not a standalone module. Surface competitor data inside the content editor research rail (Phase 7).

**Bottom of sidebar:**
- Settings (gear) → `/app/settings`
- User avatar / account

**Behavior:**
- Icon only — no labels visible (desktop)
- Tooltip appears on hover (`title` attribute, or CSS tooltip)
- Active state: icon background `#F0F0F3`, left border `2px solid var(--color-accent)`
- No expand/collapse — always icon-only on desktop (Semrush style)

### 2.3 Top Header Bar

Full-width white bar, `border-bottom: 1px solid var(--color-border)`, height `56px`. Shared across marketing, tools, and app layouts (app layout adds sidebar offset).

**Left**: Logo "Geek SEO" — `font-weight: 700`, `font-size: 18px`, `color: #1A1A2C`

**Center**: Search bar (see 1.4) — "Enter your website or keyword"

Search bar behavior by input type and auth state:

| State | Input | Action |
|-------|-------|--------|
| Logged out | URL | `GET /api/public/scan?url=` → inline results on `/` |
| Logged out | Keyword | `GET /api/public/serp-preview?keyword=` → SERP preview on `/` |
| Logged in | URL | Find or create project for domain → **Phase 1–5:** redirect to `/app/dashboard` with project row highlighted + "Audit queued" badge; **Phase 6+:** redirect to `/app/audit/[projectId]` |
| Logged in | Keyword | Navigate to `/app/keywords?q={keyword}` |

**Pre-Phase 6 fallback (critical):** Do not route authenticated URL search to `/app/audit/[projectId]` until Phase 6 ships — that page does not exist yet. Phase 1–5 uses dashboard project row + optional lightweight PSI scan via public endpoint.

**Right**:
- If logged out: "Start free →" (green CTA) + "Sign in" (text link)
- If logged in: Upgrade badge (if Starter tier) + user avatar circle (first initial)

### 2.4 Layout Grid

```
[56px sidebar] [calc(100vw - 56px) content area]
```

Content area: `max-width: none` (full width), internal padding `32px 40px`.

---

## 3. Home / Landing Page (No Login Required)

**This is the Semrush free-scan equivalent.**

### 3.1 Hero Section

Full-width white background. Centered vertically in viewport.

```
Geek SEO
AI-powered SEO for small businesses

[Enter your website or keyword ________________________] [Analyze →]

No account needed for your first site scan.
```

- H1: `font-size: 42px`, `font-weight: 700`, `letter-spacing: -0.03em`
- Subtext: `font-size: 18px`, `color: var(--color-text-secondary)`
- Search bar: large version (height `52px`)
- Below input: "No account needed" in muted text

### 3.2 Feature Cards Row

Below hero, same horizontal card row as Semrush:

| Card | Icon Color | Title | Description |
|------|-----------|-------|-------------|
| Content Optimizer | Blue | Content Optimizer | Score and improve content against real competitors |
| Topical Map | Green | Topical Map | Discover topic gaps and plan your content strategy |
| Keyword Research | Purple | Keyword Research | Find keywords your competitors rank for |
| Rankings | Orange | Rank Tracker | Track your positions in Google automatically |
| Site Audit | Red | Site Audit | Find and fix technical SEO issues |
| Analytics | Sky | Analytics | GSC + GA4 traffic and performance data |

Each card: 160px wide, `border-radius: 12px`, colored icon (40×40), title, 2-line description.

### 3.3 Free Scan Results (appears inline after submission)

**URL mode** (no login):

1. **Call**: `GET /api/public/scan?url={url}`
2. **Show loading state**: animated pulse cards for each metric section
3. **Results appear** in 3 metric blocks:

**Block 1 — Lighthouse / Page Speed** (Google PageSpeed Insights API)
- Performance score (0–100, colored gauge)
- SEO score (0–100)
- Accessibility score (0–100)
- LCP, CLS, **INP** values (PSI v5 — do not show FID)

**Block 2 — On-Page Signals** (HttpClient GET on URL, parse `<head>`)
- Title tag: value + length check (50–60 chars = good)
- Meta description: value + length check (140–160 chars = good)
- H1: present/missing
- Canonical: present/missing
- robots.txt: found/not found

**Block 3 — Teaser** (grayed out / locked)
- "Topical map: X topic clusters identified" (blurred)
- "Keyword opportunities: XX terms your competitors rank for" (blurred)
- "Content gaps: X articles your site is missing" (blurred)
- CTA: "**See your full topical map → Sign up free**"
- **Label clearly as "Preview — sign up to unlock"** until Phase 4 topical map backend exists. Values may be illustrative placeholders in Phase 2; replace with lightweight SERP-derived estimates once public SERP endpoint ships in Phase 2b-min.

**Keyword mode** (no login):

1. **Call**: `GET /api/public/serp-preview?keyword={keyword}&location={loc}` (max 10 organic results)
2. Show Google-style SERP preview: top 10 titles + URLs + featured snippet if present
3. CTA: "Track this keyword → Sign up free"

### 3.4 Sign-up CTA Banner

Below scan results:
```
[Icon] Your site scan is ready.
Sign up free to unlock your topical map, keyword gaps, and content editor.
[Create free account →]  Already have one? Sign in
```

---

## 4. Dashboard (Post-Login)

This replaces the current `/app/projects` page as the primary post-login destination. A basic dashboard already exists at `/app/dashboard` — Phase 3 rebuilds it; do not treat this as greenfield.

### 4.1 Layout

**Top**: Feature cards row (same as home, but clickable to navigate)

**CopilotAI panel** (collapsible, matches Semrush's "CopilotAI — your personal recommendations"):
- Powered by Claude
- Shows 3 AI-generated suggestions based on **content score data** (available at Phase 3)
- Examples: "Page X has a 42% content score — here are the top 3 improvements." / "Your last 3 articles are missing H2 structure."
- Cluster gap and audit-based suggestions added in Phase 7 once topical map and audit data exist
- "Open" toggle to expand

**Folders / Sites section**:
- Header: "Your sites" + "+ Add site" button
- Each site shows a domain row exactly like Semrush:
  ```
  @ domain.com  domain.com ↗
  SEO | Topical Coverage | Site Health | Organic Keywords | Backlinks
  —   |       —          |     —       |       —          |   —
  ```
- Domain row metrics (progressive enhancement — show `—` until data pipeline exists):

| Metric | Source | Available |
|--------|--------|-----------|
| **SEO** | Overall audit health score | Phase 6 |
| **Topical Coverage** | % clusters with ≥1 published page | Phase 4 |
| **Site Health** | Lighthouse SEO score | Phase 6 |
| **Organic Keywords** | GSC (if connected) or DataForSEO | Phase 3 (GSC if connected) |
| **Backlinks** | DataForSEO backlink API | Phase 2b-full or Phase 6 |

- Phase 3 ships the row **layout and skeleton** with `—` placeholders; metrics populate as Phases 4/6 complete
- Clicking a domain row expands to: Topical Map link, recent documents, audit issues (when available)

### 4.2 Recent Activity

Below sites section:
- "Recent documents" — last 5 edited content documents with score + keyword (from dashboard overview endpoint)
- "Recent audits" — last audit run date per site (Phase 6+; hidden until then)

---

## 5. Topical Map (Core New Feature)

### 5.1 What It Does

1. User creates a project with a URL
2. Background job fires automatically (or manually triggered: "Run topical analysis")
3. Job pipeline:
   a. Crawl site sitemap (`/sitemap.xml` or discover via `<link rel="sitemap">`)
   b. Extract existing pages and their content topics (NLP — reuse existing term extraction)
   c. For each identified topic cluster, run SERP analysis to find competitor coverage
   d. Cluster topics into pillars + subtopics using Claude (seed keyword → topic tree)
   e. Calculate: covered (site has content), gap (competitors have it, site doesn't), weak (site has it but low score)
4. Result stored in `seo_topical_maps` table

### 5.2 Visual Map

Interactive cluster visualization using **React Flow** (`@xyflow/react`) — node graph library, App Router compatible, MIT license. Client-only component (`'use client'`).

**Layout**: Hub-and-spoke diagram — use `@dagrejs/dagre` or `@xyflow/react` + `elkjs` for auto-layout of pillar/subtopic rings (not specified in raw React Flow; layout lib required).

- Center node: the site's primary topic (detected from homepage content or set by user)
- First ring: pillar topics (e.g., "IT Support", "Managed Services", "Cybersecurity")
- Second ring: subtopics per pillar (e.g., "IT Support for Small Business", "Remote IT Help Desk")

**Node color coding**:
- Green filled: site has content, score ≥ 70
- Yellow filled: site has content, score 40–69
- Red outline only: gap — competitors rank for this, site has no content
- Gray: low priority / out of scope

**Interactions**:
- Hover node: tooltip showing keyword, search volume, difficulty, top competitor
- Click covered node: opens content document for that page
- Click gap node: "Create content for this topic →" (creates document pre-loaded with keyword)
- Filter toggle: "Show all" / "Gaps only" / "Weak content"

**Sidebar** (right side of topical map page):
- Topic cluster list in table form (sortable by: search volume, difficulty, coverage status)
- "Set site focus" input: user can type "IT services for small business in South Florida" — Claude refines the topic clusters based on this niche
- Competitor domains detected: list of domains that appear most in the topic SERPs

### 5.3 "Set Site Focus" Feature

Input at top of topical map page:
```
What does your website focus on?
[e.g., IT services for small business in Broward County, Florida]
[Update topical map →]
```
When submitted: re-runs cluster generation with this context as a Claude system prompt parameter.

---

## 6. Site Audit + Lighthouse

### 6.1 Audit Trigger

- Auto-runs on project creation (Phase 6+)
- Manual "Re-run audit" button on dashboard and audit page
- Scheduled: weekly re-run (future)

### 6.2 What Gets Checked

**Lighthouse** (via Google PageSpeed Insights API — requires `GOOGLE_PSI_API_KEY`):
- Performance, SEO, Accessibility, Best Practices scores (0–100)
- LCP, CLS, INP
- Called for homepage URL + up to **5 key pages** (hard cap for Railway memory/time)

**On-Page Crawl** (Playwright — runs on GeekSeoBackend):
Per discovered page (max 50 pages per audit job; 10-minute job timeout):
- Title tag (present, length, keyword match)
- Meta description (present, length)
- H1 (present, count)
- Canonical tag
- Image alt texts (% missing)
- Internal links count
- Schema markup present
- Page speed (load time from Playwright)

**Technical Checks**:
- `robots.txt` accessible
- `sitemap.xml` accessible + valid
- HTTPS enforced
- Mobile viewport meta present
- No broken internal links (4xx)

**Railway constraints:** Document job timeout and page cap in worker config. Fail gracefully with partial results if timeout hit.

### 6.3 Audit Results UI

**Score overview** — 4 gauge circles: Overall, Performance, SEO, Accessibility

**Issues list** — table with:
- Issue (e.g., "Missing meta description on 4 pages")
- Impact (Critical / Warning / Info)
- Affected pages count
- Fix guidance (1 sentence)

**Pages table** — sortable list of all crawled pages with per-page scores

---

## 7. Navigation Changes

### 7.1 Remove / Replace

- `/app/projects` as primary landing (replace with `/app/dashboard`)
- Top text navigation bar (replace with left icon sidebar + top header)
- The "Create project first → then create document" friction flow

### 7.2 Route Table

| URL | Page | Sidebar | Phase |
|-----|------|---------|-------|
| `/` | Home (free scan + feature cards) | — | 2 |
| `/tools` | Free tools directory | — | 2b-min |
| `/tools/*` | Individual free tools | — | 2b-min / 2b-full |
| `/app/dashboard` | Dashboard (sites + feature cards + copilot) | Home | 3 |
| `/app/topical-map/[projectId]` | Topical map for a site | Topical Map | 5 |
| `/app/audit/[projectId]` | Site audit results | Site Audit | 6 |
| `/app/content` | All content documents (flat list, filter by project) | Content | 3 |
| `/app/content/[id]` | Editor | — | 1 (restyle) |
| `/app/keywords` | Keyword research | Keywords | 1 (restyle) |
| `/app/rankings` | GSC rankings (`GoogleProjectPanel`) | Rankings | 1 (restyle) |
| `/app/analytics` | GA4 analytics (`GoogleProjectPanel`) | Analytics | 1 (restyle) |
| `/app/settings` | Google, WordPress, subscription | Settings | 3 |
| `/app/guided` | Guided article flow | More menu | Keep |
| `/app/planner` | Keyword clustering | More menu | Keep |
| `/app/brand-voice` | Brand voice CRUD | More menu | Keep |
| `/app/briefs/new` | Brief generator | More menu | Keep |
| `/app/calendar` | Placeholder | More menu | Out of scope |
| `/app/geo` | Placeholder | More menu | Phase 8 |
| `/app/content-guard` | Placeholder | More menu | Out of scope |
| `/app/projects` | Legacy project list | Redirect → dashboard | 3 |
| `/app/projects/[projectId]` | Legacy project docs | Redirect → `/app/content?projectId=` | 3 |

### 7.3 Redirects (`frontend/next.config.ts`)

Add in Phase 1 (path renames) and Phase 3 (projects deprecation):

| From | To |
|------|-----|
| `/app/strategy/topical-map` | `/app/dashboard` (until Phase 5; then project picker) |
| `/app/audit` | ~~`/app/dashboard` (until Phase 6)~~ **Live at `/app/audit`** (May 2026) |
| `/app/projects` | `/app/dashboard` |
| `/app/projects/:projectId` | `/app/content?projectId=:projectId` |

### 7.4 New Document Creation Flow

**From dashboard**: Click "Content" feature card → documents list → "+ New document" → modal asks keyword + (optional) assign project → creates doc → opens editor. No separate project page required as a gate.

**From topical map**: Click a gap node → "Create content" → document pre-loaded with keyword and topic context from the cluster.

**Data for `/app/content` flat list:** Use `GET /api/seo/dashboard/overview` (returns all projects + documents aggregate) — not a new standalone content list endpoint. Avoid N+1 per-project fetches.

---

## 8. Backend Changes

### 8.1 New Endpoints Required

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `GET /api/public/scan?url={url}` | None | Free landing page scan: PageSpeed + on-page head data |
| `GET /api/public/serp-preview?keyword={kw}&location={loc}` | None | Keyword-mode home scan: top 10 SERP results (DataForSEO, capped) |
| `POST /api/seo/topical-map/generate` | JWT | Trigger topical map job for a project |
| `GET /api/seo/topical-map/{projectId}` | JWT | Get topical map result |
| `PATCH /api/seo/topical-map/{projectId}/focus` | JWT | Update site focus, re-run clustering |
| `POST /api/seo/audit/{projectId}` | JWT | Trigger full site audit |
| `GET /api/seo/audit/{projectId}` | JWT | Get latest audit results |
| `GET /api/seo/dashboard/overview` | JWT | Dashboard metrics: all sites, recent docs, copilot suggestions |

Public tool endpoints (Phase 2b-full): see §11.2 — added after Phase 6.

### 8.2 New Database Tables

> **Architecture note:** All EF Core migrations for these tables go on **GeekRepository** (`SeoDbContext`), not on GeekSeoBackend. GeekSeoBackend has no EF Core dependency and no database connection — persistence flows through GeekAPI internal proxy routes (see §8.5).

```sql
-- Topical map results
seo_topical_maps (
  id UUID PRIMARY KEY,
  project_id UUID REFERENCES seo_projects,
  user_id UUID,
  status TEXT NOT NULL DEFAULT 'pending',  -- pending | running | complete | failed
  generated_at TIMESTAMPTZ,
  site_focus TEXT,
  clusters JSONB,            -- { pillars: [ { topic, subtopics: [ { keyword, volume, difficulty, status, documentId? } ] } ] }
  competitor_domains TEXT[],
  error_message TEXT
)
CREATE INDEX idx_topical_maps_project_generated ON seo_topical_maps (project_id, generated_at DESC);

-- Site audit results
seo_site_audits (
  id UUID PRIMARY KEY,
  project_id UUID,
  status TEXT NOT NULL DEFAULT 'pending',  -- pending | running | complete | failed
  run_at TIMESTAMPTZ,
  lighthouse_scores JSONB,   -- { performance, seo, accessibility, bestPractices }
  issues JSONB,              -- [ { type, impact, count, pages, guidance } ]
  pages JSONB,               -- per-page crawl data
  crawled_page_count INT,
  error_message TEXT
)
CREATE INDEX idx_site_audits_project_run ON seo_site_audits (project_id, run_at DESC);
```

### 8.3 New Workers

- `TopicalMapJobWorker` — processes topical map generation queue
- `SiteAuditJobWorker` — processes audit crawl queue
- Both follow the existing `FullArticleJobWorker` pattern (background hosted service, 5s poll interval)

### 8.4 Public Scan Endpoint (no auth)

```csharp
[AllowAnonymous]
[HttpGet("api/public/scan")]
public async Task<IActionResult> Scan([FromQuery] string url, CancellationToken ct)
```

Calls:
1. Google PageSpeed Insights: `https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={url}&strategy=mobile&key={GOOGLE_PSI_API_KEY}`
2. HttpClient GET on the URL, parse `<title>`, `<meta name="description">`, `<h1>`, `<link rel="canonical">`, check `robots.txt`
3. Return combined result

Rate limit: 3 scans per IP per hour via `PublicRateLimitMiddleware` (see §8.6).

### 8.5 GeekAPI Internal Routes Required (sibling repo)

These routes must exist on GeekAPI before Phase 4/6 workers can persist data. Coordinate before starting Phase 4.

| GeekAPI Internal Route | Purpose |
|------------------------|---------|
| `GET/POST api/seo/internal/topical-map/{projectId}` | Read/write topical map results |
| `PATCH api/seo/internal/topical-map/{projectId}/focus` | Update site focus |
| `GET/POST api/seo/internal/audits/{projectId}` | Read/write site audit results |
| `GET api/seo/internal/dashboard/overview/{userId}` | Dashboard aggregate: projects, all documents, recent activity |

GeekSeoBackend implements corresponding `HttpTopicalMapRepository`, `HttpSiteAuditRepository`, and extends dashboard client to call the overview route.

### 8.6 Public Rate Limit Middleware

New `PublicRateLimitMiddleware` — IP-based sliding window using `IMemoryCache`. Separate from `SeoUsageGateMiddleware` (which requires auth context). Applied to all `/api/public/*` routes.

**Deployment note:** `IMemoryCache` is per-process. On Railway with multiple GeekSeoBackend instances, effective rate limit becomes `N × configured limit`. **MVP assumption:** single instance for public endpoints. Before scaling horizontally, migrate to Redis-backed rate limiting or accept higher effective limits.

---

## 9. Content Editor Enhancements

The editor currently lacks the research panel that makes tools like Frase valuable. These changes bring it closer to the Frase standard.

### 9.1 Left Research Rail (Phase 7)

Add a collapsible left panel to the editor (currently only right ScoreSidebar exists):

**Tabs**:
1. **Topics** — list of NLP terms extracted from competitor pages. Each term shows: term, frequency in top 10 competitors, whether term is present in current content (green checkmark vs. red X). Clicking a term adds it to a "terms to include" watchlist.

   > **External prerequisite (GeekApplication):** `ContentScoringService` and the `ScoreUpdate` model live in GeekApplication — not GeekSeoBackend. Phase 7 requires a GeekApplication release adding `nlpTerms[]` (term, competitorFrequency, presentInContent) to `ScoreUpdate`. Hub in GeekSeoBackend forwards the extended payload; hook in `useContentScoring.ts` consumes it.

2. **Questions** — PAA questions from SERP data. `DataForSEOSerpProvider` already parses `people_also_ask`; frontend types include `peopleAlsoAsk` in `seo-api.ts`. Surface from SERP cache; extend provider only if cache shape lacks PAA. Clicking a question inserts it as an H2 in the editor.

3. **Competitors** — move existing `CompetitorPanel` here, enhanced with competitor heading outlines (H1→H2→H3 tree per competitor page)

4. **Brief** — AI-generated content brief for the keyword (from existing `/api/seo/briefs/generate` endpoint, surfaced inline)

### 9.2 AI Toolbar

`components/editor/editor-ai-toolbar.tsx` exists with humanize / AI detect / auto-optimize implementations, but is **not mounted** in `content/[id]/page.tsx` as of v1.3 planning — wire it in Phase 1.

**Phase 1:** Mount `EditorAiToolbar` in the content editor page.

**Phase 7 additions** to the toolbar:
- "Generate outline" → calls `POST /api/seo/writing/outline` → inserts into editor
- "Write section" → calls `POST /api/seo/writing/draft` with selected heading as context

---

## 10. Implementation Order

Phases are ordered to minimize rework. Rough effort estimates assume one developer session per phase block.

### Phase 1 — Design System + App Shell (~3–5 days)

1. Update `frontend/src/app/globals.css`: CSS custom properties from §1.1, `@theme inline` token mapping, dark mode overrides, remove Geist dark breakage
2. Switch font from Geist to Inter in `frontend/src/app/layout.tsx`
3. Create route groups: `(marketing)`, `(tools)` shell stubs, `app/` with sidebar layout
4. Build `frontend/src/components/app/app-sidebar.tsx` (icon-only left rail, 56px, overflow menu for secondary routes)
5. Rebuild `frontend/src/app/app/layout.tsx`: sidebar + header with search bar (§2.3 fallback behavior for authenticated URL search)
6. Add `next.config.ts` redirects for `/app/strategy/topical-map`, `/app/audit` → dashboard
7. Restyle existing pages (do not rebuild logic):
   - `score-sidebar.tsx`, `content/[id]/page.tsx`, `keywords/page.tsx`
   - `rankings/page.tsx`, `analytics/page.tsx` — restyle `GoogleProjectPanel` wrapper only
   - `projects/page.tsx`, `dashboard/page.tsx`, `page.tsx`
8. **Wire `EditorAiToolbar`** into `content/[id]/page.tsx`
9. Mobile: collapsible sidebar / bottom nav, stacked header search
10. Update `frontend/CLAUDE.md`

**Deliverable:** Semrush chrome on all `/app/*` routes. All existing features still work. No route 404s from renames.

### Phase 2 — Home Page + Public Scan (~3–4 days)

1. Rebuild `frontend/src/app/page.tsx` with hero + feature cards + free scan input (URL + keyword modes)
2. Build `PublicScanController`: `GET /api/public/scan?url=` (PageSpeed + on-page head)
3. Build `GET /api/public/serp-preview?keyword=` (DataForSEO, max 10 results)
4. Add `GOOGLE_PSI_API_KEY` to `GeekSeoBackend/.env.example` and Railway env
5. Build `PublicRateLimitMiddleware` (IP-based sliding window, `IMemoryCache`; document single-instance assumption)
6. Build scan results component: Lighthouse gauges + on-page block + labeled teaser placeholders
7. Update `frontend/CLAUDE.md` and `GeekSeoBackend/CLAUDE.md`

**Deliverable:** First impression matches Semrush. Free URL and keyword scan work without login.

### Phase 2b-min — Free Tools (zero/low backend) (~2–3 days)

Ship with Phase 2 or immediately after — **before** Phase 3 dashboard.

1. Build `frontend/src/app/(tools)/tools/` directory page with card grid (link to individual tools)
2. Create `frontend/src/app/(tools)/tools/CLAUDE.md`
3. Pure-frontend tools: Word Counter, SERP Simulator, Google Review Link Generator (`search.google.com/local/writereview?placeid=`), QR Code Generator (`qrcode.react`)
4. `/tools/seo-checker` — thin wrapper reusing public scan endpoint from Phase 2
5. Tools layout: no app sidebar, header + footer signup CTA

**Deliverable:** `/tools` live with 5 tools. Zero DataForSEO cost.

### Phase 3 — Dashboard + Settings (~4–5 days)

1. Rebuild `frontend/src/app/app/dashboard/page.tsx`: feature cards + CopilotAI panel + sites section with skeleton metric rows (`—` placeholders)
2. Build `GET /api/seo/dashboard/overview` + GeekAPI internal overview route
3. Build `frontend/src/app/app/content/page.tsx` — flat document list fed by overview endpoint, filter by project
4. Build `frontend/src/app/app/settings/page.tsx` — shell aggregating `GoogleSettings`, `WordPressSettings`, subscription read
5. CopilotAI: Claude suggestions from content score data only (cluster/audit deferred to Phase 7)
6. Post-login routing: land on `/app/dashboard`; redirect `/app/projects` → `/app/dashboard`
7. Update `frontend/CLAUDE.md` and `GeekSeoBackend/CLAUDE.md`

**Deliverable:** Post-login Semrush-style dashboard. Settings accessible. Content flat list works.

### Phase 4 — Topical Map Backend (~1–2 weeks)

1. **Prerequisite:** GeekAPI internal topical-map routes live (§8.5); GeekRepository migration applied (§8.2)
2. Build `HttpTopicalMapRepository` HTTP client in GeekSeoBackend
3. Build `TopicalMapService` — sitemap crawl → topic extract → Claude cluster generation
4. Build `TopicalMapJobWorker`
5. Build `POST /api/seo/topical-map/generate` and `GET /api/seo/topical-map/{projectId}`
6. Auto-trigger on project creation
7. Update `GeekSeoBackend/CLAUDE.md`

**Deliverable:** Topical map data pipeline functional. Results stored in DB.

### Phase 5 — Topical Map Visualization (~4–5 days)

1. Install `@xyflow/react` + `@dagrejs/dagre` (or elkjs) for auto-layout
2. Build hub-and-spoke visualization component
3. Build `frontend/src/app/app/topical-map/[projectId]/page.tsx`
4. Redirect `/app/strategy/topical-map` → topical map with project picker
5. Wire "Create content" action from gap nodes
6. Add "Set site focus" input + re-clustering
7. Wire dashboard "Topical Coverage" metric column
8. Update `frontend/CLAUDE.md`

**Deliverable:** Interactive topical map. Clicking a gap creates a document.

### Phase 6 — Site Audit (~1–2 weeks)

**May 2026 — core shipped:** `SiteAuditService` (Playwright crawl, async `Task.Run`), `HttpSiteAuditRepository`, `SiteAuditController`, `/app/audit` UI. Professional tier gate. Redirect from `/app/audit` removed in `next.config.ts`.

**Still open (this plan):**

1. **Prerequisite:** GeekAPI internal audit routes live (§8.5); GeekRepository migration applied
2. ~~Build `HttpSiteAuditRepository` HTTP client~~ ✅
3. ~~Build `SiteAuditService` — Playwright crawl (50-page cap, 10-min timeout) + PageSpeed API~~ ✅ (PageSpeed wiring TBD)
4. Build `SiteAuditJobWorker` (optional — today uses in-process background task)
5. ~~Build audit results page~~ ✅ at `/app/audit` (not yet per-project `/app/audit/[projectId]`)
6. Update header search: authenticated URL → `/app/audit/[projectId]` (replaces Phase 1–5 dashboard fallback)
7. Wire audit scores into dashboard domain row (SEO, Site Health columns)
8. Update `GeekSeoBackend/CLAUDE.md`

**Deliverable:** Full site audit with Lighthouse scores + on-page issues list.

### Phase 2b-full — Free Tools (DataForSEO + AI) (~1–2 weeks)

**After Phase 6** — once audit/keyword infrastructure and cost model are validated.

1. Build `PublicToolsController` in GeekSeoBackend
2. Wire AI writing tools (thin public wrappers around existing `WritingController` endpoints — 2,000 char cap, 3 req/IP/hour)
3. Build Local SEO tools: GBP Description Generator, Local Schema Generator
4. Add keyword/SERP public endpoints (DataForSEO, capped to 10 items; daily budget guard)
5. Backlink + Authority + Traffic checkers (verify DataForSEO plan coverage first)
6. GBP Audit tool — **best-effort only**; manual input checks preferred over scraping (Google ToS risk)
7. Expand `/tools` directory with new cards
8. Update `GeekSeoBackend/CLAUDE.md`

**Deliverable:** `/tools` expanded to 10+ tools. Lead-gen funnel operational.

### Phase 7 — Editor Research Rail + CopilotAI Upgrade (~1 week)

1. **Prerequisite:** GeekApplication `ScoreUpdate` exposes `nlpTerms[]` (§0)
2. Update `SeoContentScoringHub` to forward extended `ScoreUpdate` payload
3. Update `frontend/src/hooks/useContentScoring.ts` to consume `nlpTerms`
4. Add left research panel to `content/[id]/page.tsx` (Topics, Questions, Competitors, Brief tabs)
5. Move `CompetitorPanel` into research rail; remove duplicate from right column
6. Wire "Generate outline" and "Write section" in `editor-ai-toolbar.tsx`
7. Upgrade CopilotAI on dashboard: cluster gap + audit-based suggestions
8. Update `frontend/CLAUDE.md` and `GeekSeoBackend/CLAUDE.md`

**Deliverable:** Editor has Frase-style research panel. CopilotAI uses full site data.

### Phase 8 — AI Visibility / GEO (future)

- Free tool: `/tools/ai-visibility` — brand name + URL → query Claude with structured extraction
- Upgrade `/app/geo` from placeholder
- Defer until Phases 1–7 complete

---

## 11. Free Tools Hub (Lead-Gen Strategy)

Semrush drives signups through free standalone tools. Geek SEO replicates this at `/tools`.

### 11.1 Tools Page

`/tools` — full-width page (tools layout, no sidebar), organized in three sections:

```
Free SEO Tools | Free AI Writing Tools | Free Local SEO Tools
```

Each tool card: icon, name, one-line description, "Try free →" CTA.

### 11.2 Free SEO Tools

| Tool | URL | Backend Source | Phase |
|------|-----|---------------|-------|
| **SEO Checker** | `/tools/seo-checker` | Public scan (§8.4) | 2b-min |
| **Google SERP Simulator** | `/tools/serp-simulator` | Pure frontend | 2b-min |
| **Word Counter** | `/tools/word-counter` | Pure frontend | 2b-min |
| **Keyword Tool** | `/tools/keywords` | `GET /api/public/keywords?q=` → DataForSEO | 2b-full |
| **Keyword Volume Checker** | `/tools/keyword-volume` | Same endpoint | 2b-full |
| **Keyword Rank Checker** | `/tools/rank-checker` | Public SERP lookup | 2b-full |
| **SERP Checker** | `/tools/serp` | `GET /api/public/serp?keyword=` | 2b-full |
| **Competitor Finder** | `/tools/competitors` | Public SERP → top domains | 2b-full |
| **Backlink Checker** | `/tools/backlinks` | DataForSEO Backlinks API | 2b-full |
| **Website Authority Checker** | `/tools/authority` | DataForSEO Domain Overview | 2b-full |
| **Website Traffic Checker** | `/tools/traffic` | DataForSEO Traffic Analytics | 2b-full |
| **Sitemap Generator** | `/tools/sitemap` | Playwright/sitemap crawl | 2b-full |
| **Plagiarism Checker** | `/tools/plagiarism` | `POST /api/public/writing/detect` | 2b-full |

**Notes:**
- All API-backed tools: 1 free result, then "Sign up to run unlimited checks"
- Rate limit: 5 requests/IP/hour per tool via `PublicRateLimitMiddleware`
- DataForSEO: cap results to 10 items; daily call budget in `IMemoryCache` — return 503 when exceeded
- Verify backlink/authority/traffic endpoints in current DataForSEO plan before Phase 2b-full

### 11.3 Free AI Writing Tools (Phase 2b-full)

Public wrappers around existing `WritingController` endpoints via `PublicToolsController`:

| Tool | URL | Backend Endpoint |
|------|-----|-----------------|
| **AI Text Generator** | `/tools/ai-writer` | `POST /api/public/writing/generate` |
| **AI Title Generator** | `/tools/title-generator` | `POST /api/public/writing/titles` |
| **Summary Generator** | `/tools/summarizer` | `POST /api/public/writing/summarize` |
| **Paragraph Rewriter** | `/tools/rewriter` | `POST /api/public/writing/rewrite` |
| **Paraphrasing Tool** | `/tools/paraphrase` | Same rewrite endpoint, different tone |
| **Sentence Rewriter** | `/tools/sentence-rewriter` | Same rewrite endpoint, sentence mode |

Caps: 2,000 char input, 3 req/IP/hour, no document saved.

### 11.4 Free Local SEO Tools (Phase 2b-full)

| Tool | URL | Implementation |
|------|-----|---------------|
| **GBP Description Generator** | `/tools/gbp-description` | Claude: business name + category + location → 750-char description |
| **Google Review Link Generator** | `/tools/review-link` | Place ID → `https://search.google.com/local/writereview?placeid={PLACE_ID}` |
| **Google Review QR Code** | `/tools/review-qr` | Review link + `qrcode.react` PNG download |
| **Local Schema Generator** | `/tools/local-schema` | Business info → LocalBusiness JSON-LD |
| **Google Business Profile Audit** | `/tools/gbp-audit` | **Best-effort** — manual checklist preferred; scraping flagged as ToS risk |

Also shipped in Phase 2b-min: Review Link Generator, QR Code Generator (pure frontend).

### 11.5 AI Search Visibility (GEO) — Phase 8

See Phase 8 in §10. Keep `/app/geo` as placeholder until then.

---

## 12. Out of Scope (This Plan)

- AI Visibility / GEO full implementation — Phase 8
- Content calendar — remains placeholder in More menu
- Content guard — remains placeholder in More menu
- Full mobile responsive audit — Phase 1 ships minimum viable responsive; full audit deferred
- Multi-user / team features — future
- Billing / subscription UI changes — future (subscription read in settings only)
- True plagiarism detection (not AI detection) — needs third-party API
- Redis-backed rate limiting — future (document single-instance MVP assumption in §8.6)

---

## Review Checklist

- [ ] Color palette and font match the Semrush screenshot reference
- [ ] Dark mode token overrides defined (§1.1) — not left as broken Geist dark
- [ ] Mobile minimum responsive rules in Phase 1 (§1.4)
- [ ] Layout groups separate `/`, `/tools`, `/app` (§2.1)
- [ ] Icon sidebar matches Semrush thin left rail; Competitors removed from sidebar
- [ ] Existing routes preserved via More menu + redirects (§7.2, §7.3)
- [ ] Free URL + keyword scan work without login (§3.3, §8.1)
- [ ] Authenticated header search does not 404 before Phase 6 (§2.3 fallback)
- [ ] Dashboard domain rows ship as skeleton in Phase 3; metrics populate in Phases 4/6 (§4.1)
- [ ] `/app/content` flat list uses dashboard overview endpoint (§7.4)
- [ ] `/app/settings` built in Phase 3
- [ ] EditorAiToolbar wired in Phase 1 (§9.2)
- [ ] Topical map covers Frase/Semrush gap; React Flow + layout lib specified (§5.2)
- [ ] Phase 2b split: 2b-min (5 tools) before dashboard; 2b-full after Phase 6
- [ ] Backend tables include job status + indexes (§8.2)
- [ ] GeekAPI internal routes coordinated before Phase 4/6 (§8.5)
- [ ] `GeekSeo.Application` `nlpTerms[]` coordinated before Phase 7 (§0, §9.1; see PLATFORM-DECOUPLING M2)
- [ ] Rate limiting: IMemoryCache single-instance assumption documented (§8.6)
- [ ] `GOOGLE_PSI_API_KEY` on Railway before Phase 2
- [ ] Phase 3 CopilotAI scoped to content-score data; full data in Phase 7
- [ ] Google Review Link uses `search.google.com/local/writereview?placeid=` (not `g.page/r/`)
- [ ] Free scan teaser labeled as preview until Phase 4 (§3.3)
- [ ] Audit job: 50-page cap, 10-min timeout documented (§6.2)
- [ ] CLAUDE.md updates included as last step of every phase

---

*Plan version: 1.3 — critique incorporated 2026-05-29*
