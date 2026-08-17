# Crawler architecture

Standing description of what the crawler **is**. Not a plan. Fetch/render is Phase 1 of Site Analyzer
performing exactly as a search engine (index, ranking, query-serving, and crawl-budget/scheduling
are not here — see "Not in this document" in `docs/plans/mobile-first-crawl-part-3-render-always.md`).

## Two layers

| Layer | Job | Opinion |
|---|---|---|
| **Fetch / render** | Produce a crawl record: rendered mobile DOM + protocol facts | Mirror Googlebot Smartphone fetch. No extraction choices. |
| **Extraction** | Heading tree, word counts, topicality, tool lists **on that tree** | Our content model. Reads `data-gsv`; never mutates the captured DOM. |

**Rule:** no extraction decisions inside the fetch layer. Ever. Preferring the `aria-hidden` sizer
word, skipping `desktop-only` copy in the tree, grouping tool anchors — those belong in extraction.

Tool lists are **links on the heading tree** (`TreeJson`), not a separate `extracted_tools` table
(dropped). Content Creator Generate Tools queries those trees by `site_analysis_profiles.Id`.

## What we mirror

- Render always (Playwright/Chromium). No HTTP-HTML crawl mode. Chromium down → named failure.
- Evergreen Chromium. Device emulation is Playwright <c>devices['Pixel 7']</c> (viewport 412×839,
  screen 412×915, DPR 2.625, touch, Pixel 7 Chrome UA) plus a `GeekSEO/1.0` product token.
  That is not a “Pixel engine” — Chromium paints; the descriptor is the phone.
- Navigate to `load`, then network-idle (no connections for 500ms) capped at 5s. Lesson from
  commit `4564782`: a 15-minute Analyzer job is a **breadth** budget, not a reason to snapshot at
  400ms or to hang forever on `networkidle`.
- Keep the DOM. Annotate visibility (`data-gsv`); never `el.remove()`.
- RFC 9309 robots.txt. 4xx robots → allow-all; 5xx → fail closed (disallow-all).
- Record `Url`, `FinalUrl`, `StatusCode`, `RedirectChain`, `Canonical`, `NoIndex` / `NoFollow`.
  `NoIndex` means *indexed: no, crawled and followed: yes*.
- Same-site identity: `www`/apex and `http`/`https` are one site; strip `utm_*` / click ids.
- Uncapped same-origin BFS. robots.txt is the skip mechanism (no `HardJunkPaths`).

## Divergences (exceptions only)

A divergence from search-engine fetch must be argued, dated, and listed here. If it is not in this
table, it is a bug.

| Divergence | Why | Cost | Date |
|---|---|---|---|
| Abort ads/analytics/beacon requests | No layout effect; no indexing value to Google either | Effectively none | 2026-08 |
| No conditional-GET / recrawl scheduling yet | Deferred — crawl record is the seam | Re-crawls refetch unchanged pages | 2026-08 |
| PDFs and images recorded in inventory but not text-extracted | Google indexes PDF text; we capture URL/content-type | PDF *content* absent from analysis | 2026-08 |

**Not divergences**

- Honest `GeekSEO/1.0` UA. Search engines identify themselves. Impersonating Googlebot is not
  mirroring.
- Preferring the `aria-hidden` sizer's complete word. Fetch captures both spans; extraction chooses.

**Rejected outright** (do not reintroduce): blocking images/fonts; shortening the render budget
below the quiescence cap to "make Analyze finish"; dropping `collapsed` content from the tree;
hardcoded path/extension skip lists as a substitute for robots.txt.

## `data-gsv` contract

Set only by the fetch layer, after a **mobile snapshot**, by comparing computed visibility at the
mobile viewport and a desktop probe (1280). Dual-viewport is **our** classification on Google's
mobile document — Googlebot itself does not resize to desktop.

Nearest annotation wins. Consumers must respect it. New HTML readers that ignore `data-gsv` are bugs.

| | Googlebot | Us |
|---|---|---|
| CSS-hidden at mobile **and** desktop (accordion, tab, nav drawer) | Renders, indexes at **full weight**, follows the links | `collapsed` — kept in the content tree |
| CSS-hidden at mobile, **shown** at desktop (`hidden lg:block`) | Not in the mobile index | `desktop-only` — kept in the DOM for links, excluded from tree text |
| Not in the DOM until interaction | Never sees it (no click/tap/scroll) | Same |

Row two is why **no heading dedupe** is needed: Google renders one hero. Verified Aug 2026 against
Google Search Central mobile-first indexing and public crawler guidance (Illyes/Mueller).

## Crawl record

`CrawledPage` is the indexer seam. Downstream (Site Analyzer trees, Content Creator hierarchy match,
future index) reads records. It does not define fetch.

## Timing (fill in after a live recrawl)

Record wall-time on `geekatyourspot.com` before/after this rewrite here so the 15-minute job budget
is not re-litigated from guesswork. Per-page cost is expected to rise (quiescence cap, no
image/font blocking, dual-viewport probe). If the job budget binds, bound **breadth**, not fidelity.
