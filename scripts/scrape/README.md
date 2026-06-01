# geek-scrape

A **generic** browser scraper: point it at **any URL** and save structured artifacts for **competitive analysis** and **code planning**.

**Commands match plain English:**

| Command | Meaning |
|---------|---------|
| **`site`** | Crawl a whole site (**URLs from sitemap.xml**, up to `--max-pages`) |
| **`page`** | Scrape **one** URL only (`content.md` at the output root) |
| **`links`** | List same-origin URLs from a page (discovery only) |

Typical uses:

- **Competitor analysis** — capture how a rival product presents rank tracking, audits, pricing, or feature matrices.
- **Feature & component cloning** — study screens and reimplement patterns in GeekSEO (Shadcn + your APIs).
- **Code planning** — feed `content.md` + `page.json` into specs under `docs/research/`.

This tool does **not** replace **official APIs** for production data (SERP, keywords, backlinks). Runtime data strategy: [`plan-documents/GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md`](../../plan-documents/GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md).

## Install

```bash
npm install
npm run scrape:setup
```

## Quick start

```bash
# Whole public site (sitemap URLs, up to 30 pages, 1.5s between requests)
npm run scrape:seranking

# Homepage only — one URL
npm run scrape:seranking:page

# One feature screen
npm run scrape:seranking:rank-tracker
```

**Do not use `example.com` for research** — blocked unless `--smoke`. Use `page` for the smoke test:

```bash
npm run scrape -- page --url "https://example.com" --out ./docs/research/competitors/_test/example --smoke
```

npm needs `--` before flags:

```bash
npm run scrape -- site --url "https://seranking.com/" --out ./docs/research/competitors/seranking --network
npm run scrape -- page --url "https://seranking.com/pricing.html" --out ./docs/research/competitors/seranking/pricing --network
```

Legacy: `crawl` is an alias for `site`.

## Options

| Flag | Description |
|------|-------------|
| `--max-pages` | Site crawl cap (default **30**) |
| `--delay-ms` | Pause between pages on **site** (default **1500**) |
| `--link-crawl` | If sitemap is empty/missing, discover via HTML links instead of failing |
| `--network` | Save XHR/fetch URLs to `network.json` |
| `--selector` | CSS root (default: `main`, `article`, or `body`) |
| `--full-page` | Full-page screenshot (**page** only) |
| `--wait` | Extra ms after load (default 2500) |
| `--smoke` | Allow example.com (test only) |

## Output: `site` (whole site)

**Start with `SITE-REPORT.md`.**

Discovery reads `robots.txt` `Sitemap:` lines, then `/sitemap.xml` (and common variants), including nested sitemap indexes.

```text
<out>/
  SITE-REPORT.md
  site-index.json
  sitemap-sources.json
  sitemap-urls.json
  manifest.json
  pages/<slug-N>/
    content.md
    page.json
    full-text.txt
    screenshot.png
    network.json       # if --network
```

Pages are taken from the sitemap in order (start URL first when listed). No login or paywall bypass.

## Output: `page` (one URL)

**Start with `SCRAPE-REPORT.md`.**

```text
<out>/
  SCRAPE-REPORT.md
  content.md
  page.json
  full-text.txt
  raw.html
  links.json
  screenshots/viewport.png
  network.json       # if --network
```

## Related

| Tool | Purpose |
|------|---------|
| `npm run scrape` | This CLI |
| `.agents/skills/deconstruct-web-feature/` | Spec → Shadcn implementation |
| `scripts/deconstruct/extract-feature.mjs` | Legacy; prefer `geek-scrape page` |
