# Reference site: geekatyourspot.com (Geek SEO dogfood project)

**Last scanned:** 2026-06-06 (HTTP + homepage HTML)  
**Geek SEO project URL:** typically `https://www.geekatyourspot.com` on the linked `seo_projects` row.

This document is the **ground truth** for what the public site exposes today and what Niche Analyzer v1 can actually see. Use it when testing Niche Analyzer, writing UI copy, or planning crawl improvements.

---

## Site stack (not WordPress-only)

| Signal | Value |
|--------|--------|
| Platform | **Next.js** (`/_next/static/…` assets on homepage) |
| Primary URL | `https://www.geekatyourspot.com` |
| Locale variant | `https://www.geekatyourspot.com/en-US` |
| Crawlable paths (from homepage links) | `/`, `/privacy-policy`, `/terms-and-conditions`, `/en-US` |
| In-page nav | Mostly **hash anchors** (`#consultationAppointment…`) — not separate URLs |
| `robots.txt` | Allows `/`; **Disallow** `/api/`, `/about/`, `/contact/`, `/services/`, `/temp/` |
| `/services/` HTTP | **404** (path disallowed in robots and not a live route today) |

Do **not** describe this site as “WordPress with a fat sitemap.” The sitemap is minimal because the app is a **single-page marketing site** with schema on the homepage, not because of a generic WordPress limitation.

---

## Sitemap (live)

`GET https://www.geekatyourspot.com/sitemap.xml` (2026-06-06):

```xml
<urlset>
  <url>
    <loc>https://www.geekatyourspot.com</loc>
    <lastmod>2026-06-06T12:53:16.547Z</lastmod>
  </url>
</urlset>
```

**One URL.** `SitemapExtractor.GroupIntoPillars` only creates pillars when a URL has **at least two path segments** (e.g. `/services/ai-consulting`). Homepage-only entries produce **zero sitemap pillars** — by design in code, not because extraction failed.

---

## Schema.org JSON-LD (homepage)

Single `LocalBusiness` + `ProfessionalService` block (`@id` … `#business`):

| Field | Values |
|-------|--------|
| **name** | Geek at Your Spot |
| **description** | AI consulting firm helping small businesses in South Florida implement AI, process automation, chatbots, and data analytics. |
| **knowsAbout** (7) | Artificial Intelligence, Process Automation, AI Chatbots, Data Analytics, AI Strategy Consulting, Security and Compliance, Web Application Development |
| **areaServed** (3) | Broward County FL, Palm Beach County FL, Miami-Dade County FL |
| **telephone** | +1 561-526-3512 |

These `knowsAbout` strings are how we know pillars like “Artificial Intelligence” and “Process Automation” — **from live JSON-LD**, not from assumption.

---

## What Niche Analyzer v1 reads (crawl scope)

**Implemented today — not a full-site crawl:**

| Source | Scope | geekatyourspot.com result |
|--------|--------|---------------------------|
| Schema.org | Homepage JSON-LD (HTTP; Playwright if browser up) | 7 topics + 3 counties + description |
| Sitemap | Up to 5 000 URLs from sitemap index/children | 1 URL → **0 pillars** |
| Navigation | Playwright: `nav a`, header links, mobile menu | Few same-origin links; no service silos |
| Headings | Homepage title, meta, H1–H6 | Homepage content only |

**Not implemented in v1:** follow internal links across the domain, crawl disallowed paths, or fetch every rendered section as its own URL. See [`docs/reference/site-niche-analyzer-v1-spec.md`](../../docs/reference/site-niche-analyzer-v1-spec.md) and [`plan-documents/NICHE-ANALYZER-ARTIFACT-PARADIGM.md`](../../plan-documents/NICHE-ANALYZER-ARTIFACT-PARADIGM.md).

For a **whole-site picture** outside the product, use `scripts/scrape` with `--link-crawl` when the sitemap is thin.

---

## Expected Niche Analyzer output (SUL `sul-2.0`)

When analysis runs successfully against this domain:

| Step | Expected finding |
|------|------------------|
| 1 Schema | 7 `knowsAbout` + 5 offer catalog / `serviceType` topics → **12 schema candidates** |
| 2 Sitemap | `totalUrls = 1`, `pillars = []` |
| 3 Nav | 0–2 weak pillars or empty |
| 4 Headings | Title + H2s from homepage |
| 5 Page content | H3 verticals incl. **Accounting** (page-only) |
| 6 Structure | Limited crawl (thin sitemap); internal links from homepage |
| 7 Select | **13 pillars** typical; `sulVersion = sul-2.0`; all 12 schema topics **unconditionally selected**; `page_vertical` topics ≥ 0.15 |
| 8–9 | Keyword + SERP when providers configured |
| 11 Local | 3 counties in `areaServed`; location-page gaps if no `/locations/*` |
| 12 Coverage | Partial/gap from URL matching (single-URL site) |
| 13–14 | Authority score + persist `SiteTopicProfile` snapshot |

**Why 13 pillars:** All 12 distinct JSON-LD topics kept (schema = unconditional). **Accounting** from homepage H3 — not in schema — via `page_vertical` evidence (0.28 ≥ 0.15 threshold). Heading-only noise (0.10) excluded.

## Expected Niche Analyzer output (SUL `sul-1.3`, deprecated)

When analysis runs successfully against this domain:

| Step | Expected finding |
|------|------------------|
| 1 Schema | 7 `knowsAbout` + 5 offer catalog / `serviceType` topics → **12 schema candidates** |
| 2 Sitemap | `totalUrls = 1`, `pillars = []` |
| 3 Nav | 0–2 weak pillars or empty |
| 4 Headings | Title + H2s from homepage |
| 5 Page content | H3 verticals incl. **Accounting** (page-only) |
| 6 Structure | Limited crawl (thin sitemap); internal links from homepage |
| 7 Fuse | **13–15 pillars** typical; `fusionVersion = sul-1.3`; provenance per candidate |
| 8–9 | Keyword + SERP when providers configured |
| 11 Local | 3 counties in `areaServed`; location-page gaps if no `/locations/*` |
| 12 Coverage | Partial/gap from URL matching (single-URL site) |
| 13 Persist | `FusedSiteUnderstanding` JSON on profile |

**Why 13+ pillars:** All 12 distinct JSON-LD topics kept. **Accounting** from homepage H3 — not in schema — via `page_vertical` evidence.

---

## Expected Niche Analyzer output (v1, legacy merge — deprecated)

When analysis runs successfully against this domain:

| Step | Expected finding |
|------|------------------|
| 1 Schema | 7 `knowsAbout` → 7 schema pillars (`ChildPageCount = 3`, no child slugs) |
| 2 Sitemap | `totalUrls = 1`, `pillars = []` |
| 3 Nav | 0–2 weak pillars (e.g. privacy/terms) or empty |
| 4 Headings | Title + H2s from homepage; may supplement if schema empty |
| 5 Merge | Up to **7** schema pillars (cap); areaServed **not** added as pillars when count ≥ 3 |
| 6 Build + score pillars | All pillars `coverageStatus = gap`, `coveredSubtopicCount = 0` |
| 7 Authority | Low score (breadth term is 0 until coverage matching exists) |
| 8–10 Persist | ~7 pillars × (5 generic subtopics each if &lt; 3 real child slugs) |

**Niche tags** (header): up to 3 counties + 3 pillar names from `BuildNicheTags`.  
**Primary niche string:** from `NicheRootEntityBuilder` (brand + top pillars + location).

---

## Product gaps relevant to this site

1. **Thin sitemap** — single URL; multi-page crawl only helps when sitemap lists more paths.
2. **Local radius** — Step 11 uses schema `areaServed` + location URLs; **address + 20 mi radius** (Phase 2+) not wired yet.
3. **Full-site link crawl** — bounded by sitemap sample + homepage links, not entire domain.

**Shipped (SUL):** fusion provenance, keyword/SERP enrichment (when providers on), GSC overlay, content coverage matcher, action recommendations.

---

## Related files

- Spec (v1): [`docs/reference/site-niche-analyzer-v1-spec.md`](../../docs/reference/site-niche-analyzer-v1-spec.md)
- Artifact paradigm: [`plan-documents/NICHE-ANALYZER-ARTIFACT-PARADIGM.md`](../../plan-documents/NICHE-ANALYZER-ARTIFACT-PARADIGM.md)
- Orchestrator: `GeekSeoBackend/Services/NicheAnalyzerService.cs`
- Scraper (research): `scripts/scrape/README.md`
