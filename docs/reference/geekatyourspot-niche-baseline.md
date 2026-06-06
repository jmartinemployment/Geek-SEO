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

**Not implemented in v1:** follow internal links across the domain, crawl disallowed paths, or fetch every rendered section as its own URL. v1.5 work is scoped in [`plan-documents/SITE-NICHE-ANALYZER-CHANGES.md`](../../plan-documents/SITE-NICHE-ANALYZER-CHANGES.md).

For a **whole-site picture** outside the product, use `scripts/scrape` with `--link-crawl` when the sitemap is thin.

---

## Expected Niche Analyzer output (v1.5+, fusion `sul-1.0`)

When analysis runs successfully against this domain (after June 2026 deploy):

| Step | Expected finding |
|------|------------------|
| 1 Schema | 7 `knowsAbout` + 5 offer catalog / `serviceType` topics → **12 schema candidates** |
| 2 Sitemap | `totalUrls = 1`, `pillars = []` |
| 3 Nav | 0–2 weak pillars or empty |
| 4 Headings | Title + H2s from homepage |
| 5 Fuse | **~11 pillars** selected (Gate 2 merges e.g. AI Strategy Consulting + AI Consulting); cap 12; `fusionVersion = sul-1.0` |
| 6–10 | Profile, scoring, persist |

**Why not 12 pillars:** similarity merge collapses near-duplicate AI consulting topics before the cap applies.

---

## Expected Niche Analyzer output (v1, legacy merge)

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

1. **Full-site crawl** — needed to discover content not listed in sitemap (and to score real coverage).
2. **areaServed always visible** — today counties appear in **tags** but become **pillars** only when total pillars &lt; 3 after merge.
3. **Coverage matching** — no pass that maps existing URLs/pages to subtopics; everything stays `gap`.
4. **Keyword metrics** — `IKeywordProvider` not wired in niche pipeline; volume/KD/quick wins inactive.

---

## Related files

- Spec (v1): [`plan-documents/SITE-NICHE-ANALYZER.md`](../../plan-documents/SITE-NICHE-ANALYZER.md)
- Changes (v1.5): [`plan-documents/SITE-NICHE-ANALYZER-CHANGES.md`](../../plan-documents/SITE-NICHE-ANALYZER-CHANGES.md)
- Orchestrator: `GeekSeoBackend/Services/NicheAnalyzerService.cs`
- Scraper (research): `scripts/scrape/README.md`
