# Site Analyzer ↔ Content Writing

**Status:** Shipped in Geek-SEO repo (GeekSeoBackend + frontend). Persistence routes require DATA_API contract below.

## Rule

**No complete Site Analyzer pack → Content Writing is blocked everywhere** (setup, review, draft, Insights). There is no keyword-only path, no URL Analyzer path, and no operator override.

## Product split

| Surface | Owns | Never owns |
|---------|------|------------|
| **Site Analyzer** | 10 manual steps; site index (`seo_site_research` + pages); keyword pack on `seo_url_research` with `site_research_id` | Draft HTML, scoring sidebar |
| **Content Writing** | Attach finalized pack (`urlResearchId` where `data_quality = full`); draft + Insights from frozen pack | Live SERP, crawl steps, partial packs |

## Handoff contract

Content Writing accepts a pack only when **all** are true:

1. `seo_url_research.status = completed`
2. `seo_url_research.data_quality = full`
3. `seo_url_research.site_research_id` is set (site index steps 1–4 green)
4. `SiteAnalyzerPackValidator` gate minimums pass (organic ≥1, PAA ≥1, PASF ≥1, PAF present or explicit `none`, competitors ≥3, terms ≥8, section hints ≥4, closing FAQs = 5, business context merged)

Block UX copy: **"Site must be crawled first."** plus the failing gate message and a link to `/projects/{projectId}/site-analyzer`.

## API (GeekSeoBackend)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/seo/site-analyzer/projects/{projectId}/state` | Wizard state + step colors |
| POST | `/api/seo/site-analyzer/projects/{projectId}/site-index/steps/{1-4}/run` | Manual site index step |
| POST | `/api/seo/site-analyzer/projects/{projectId}/packs` | Create keyword pack (`keyword`, `location?`) |
| POST | `/api/seo/site-analyzer/packs/{urlResearchId}/steps/{5-10}/run` | Manual keyword-pack step |

Retired: `POST /api/seo/url-research/analyze` → **410 Gone**.

## Persistence (GeekRepository / DATA_API)

GeekSeoBackend calls these internal routes via `HttpSiteResearchRepository`:

| Route | Purpose |
|-------|---------|
| `POST api/seo/internal/site-research?userId=` | Get or create site index for project |
| `GET api/seo/internal/site-research/{id}?userId=` | Site index + pages |
| `PUT .../step1` | Persist discovered URLs (jsonb) |
| `PUT .../pages` | Replace crawled/extracted page rows |
| `PUT .../step4` | Business summary + internal link map |
| `PUT api/seo/internal/site-analyzer/step-runs?userId=` | Upsert step run (green/red + message + log) |
| `GET api/seo/internal/site-analyzer/step-runs?siteResearchId=` or `urlResearchId=` | List step runs |

Tables (EF migration `AddSiteResearchTables` in this repo):

- `geek_seo.seo_site_research`
- `geek_seo.seo_site_research_page` (`headings_json`, `json_ld_json` as jsonb)
- `geek_seo.seo_site_analyzer_step_run`

Keyword pack child rows remain on existing `seo_url_research_*` tables.

## vs Niche Analyzer

Shared crawl primitives (`SitePageCrawler`, `SitemapExtractor`, etc.). Niche = strategy profile. Site Analyzer = **writing dependency**. Content Writing does not read niche profile.

## vs URL Analyzer (removed)

URL Analyzer and keyword-only Content Writing entry points are deleted. `/url-analyzer` redirects to `/site-analyzer`.
