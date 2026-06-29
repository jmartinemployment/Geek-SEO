# ADR 004: Crawl Bounds

## Status
Accepted

## Defaults
| Parameter | Value |
|-----------|-------|
| max_crawl_depth | 4 |
| max_crawl_pages | 150 |
| CRAWL_STAGE_TIMEOUT | 10 minutes |

**Target site:** BFS from `TargetSiteUrl` stops at first limit hit (`projects.max_crawl_depth` / `max_crawl_pages`).

**Included SERP URLs (implementation today):** Fetched **outside** the target BFS page cap, but only as **one HTTP GET per SERP URL** — not a multi-page crawl per competitor domain. There is no competitor-site BFS in `PageFetchService` today. Competitor `pages` rows (`IsTargetSite = false`) are often absent in production.

**Planned:** Additive per-domain competitor BFS — [010-competitor-crawl-planned.md](010-competitor-crawl-planned.md).

## Rationale
150 pages × ~2s ≈ 5 min within 10 min timeout. Depth 4 covers typical SMB architecture without catalog blow-up.
