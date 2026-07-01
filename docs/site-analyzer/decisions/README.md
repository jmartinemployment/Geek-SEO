# Architecture decision records

## Operator workflow (current)

| ADR | Topic |
|-----|--------|
| [008-serp-scraper.md](008-serp-scraper.md) | Manual Chrome HTML import in production (`SERP_EXECUTION=manual`) |
| [010-competitor-crawl-planned.md](010-competitor-crawl-planned.md) | Competitor deep crawl — **required** on operator path |
| [011-no-rest-polling.md](011-no-rest-polling.md) | **No REST polling** for crawl progress — SSE + Channels + Postgres NOTIFY |
| [012-operator-research-model.md](012-operator-research-model.md) | Pillars, gap topics, comparison, `analysisRunId` handoff — operator path |
| [016-manual-five-lane-research.md](016-manual-five-lane-research.md) | Manual HTML lanes (keyword/edu/gov/local/wiki), dual write gates, `sa2` persistence |
| [014-product-phases-frase-first.md](014-product-phases-frase-first.md) | **Frase phase first** — research pack to Writer; no SA2 outlines; no Semrush drift |

Canonical glossary: [RESEARCH-MODEL.md](../RESEARCH-MODEL.md). Active plan: [plans/frase-phase.md](../plans/frase-phase.md).

## Legacy in-repo pipeline (not operator path)

These describe the step-gated `/runs/*` pipeline and Web UI era. Code may remain; daily workflow does not use them.

| ADR | Topic |
|-----|--------|
| [001-fetch-strategy.md](001-fetch-strategy.md) | HTTP vs Playwright fetch |
| [002-serp-providers.md](002-serp-providers.md) | Provider interface (superseded by 008 for production) |
| [003-relevance-filter.md](003-relevance-filter.md) | SERP filter stage |
| [004-crawl-bounds.md](004-crawl-bounds.md) | Target-site crawl limits |
| [005-bounded-pagerank.md](005-bounded-pagerank.md) | PageRank stage |
| [007-signalr-channel.md](007-signalr-channel.md) | Run progress hub (legacy Web UI) |
| [009-business-focus-extraction.md](009-business-focus-extraction.md) | Business profile after Extract |

Removed: ADR 006 (frontend stack) — Web app removed from repo.

Canonical historical spec: [archive/SiteAnalyzer-PLAN.md](../archive/SiteAnalyzer-PLAN.md).
