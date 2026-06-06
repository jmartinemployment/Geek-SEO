# Geek SEO — Planning

## Start here

| Document | Purpose |
|----------|---------|
| **[`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md)** | **Provider strategy** — SerpApi bridge, Geek-owned SERP/crawl, DataForSEO → zero |
| **[`TODO.md`](TODO.md)** | **All remaining work** (scoring v2, #12b, waivers, integrations, REDESIGN, ops, security) |
| **[`PROJECT_STATUS.md`](../PROJECT_STATUS.md)** | What’s live in production; v1 parity #1–27 status |
| **[`docs/ROADMAP.md`](../docs/ROADMAP.md)** | One-screen index |
| **[`ARCHITECTURE.md`](ARCHITECTURE.md)** | Services, ports, API surface |

## Product upgrade specs (reference)

| File | Purpose |
|------|---------|
| [`SEARCH-UNDERSTANDING-LAYER.md`](SEARCH-UNDERSTANDING-LAYER.md) | **North star** — public-signal composite approximating shared search-engine understanding; fusion architecture + phased rollout |
| [`TopicalMapUpgrade.md`](TopicalMapUpgrade.md) | Topical map UX gaps — data deps per [`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md) |

## Deep dives (reference only)

| Document | Purpose |
|----------|---------|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Services, ports, data flow |
| [`PLATFORM-DECOUPLING.md`](PLATFORM-DECOUPLING.md) | M0–M9 (complete) |
| Scoring (code) | `GeekSeo.Application/Services/ContentScoringService.cs` |
| [`competitor-analysis.md`](competitor-analysis.md) | Competitor research |
| [`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md) | Same as Start here (canonical provider plan) |
| [`KEYWORD-DISCOVERY-STRATEGY.md`](KEYWORD-DISCOVERY-STRATEGY.md) | `IKeywordDiscoveryProvider` — Phase B companion |

## Do not use for planning

- [`DATAFORSEO-REPLACEMENT-UPGRADE.md`](DATAFORSEO-REPLACEMENT-UPGRADE.md), [`GEEK-DATA-PLANE.md`](GEEK-DATA-PLANE.md) — redirect stubs only.
- Retired vendor-expansion content (Labs / Backlinks / OnPage) is **not** coming back.
- Canonical: **[`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md)**.

`REDESIGN-PLAN.md`, `PARITY-SPECS.md`, `API-CONTRACTS.md` — UX/reference only if present in the tree.
