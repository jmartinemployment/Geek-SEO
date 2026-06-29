# ADR 010: Competitor deep crawl (additive)

## Status
Accepted — implemented as separate API and tables.

## Amendment (operator path — seed-only)

The **operator path** (`site-analyzer.geekatyourspot.com`) uses **seed-only** crawl: **one ranking URL per domain** from organic SERP. No whole-site BFS per competitor domain.

Evidence: `CompetitorCrawlService` picks the best seed per domain (`SeedRankAbsolute`); crawl-eligible rows via `SerpCrawlEligibility` (Included + PendingReview).

Legacy depth/page limits in this ADR apply to the in-repo step-gated pipeline, not the operator UI path.

## Context

- Working SiteAnalyzer: manual SERP HTML → `serp_items` ([OPERATOR-WORKFLOW.md](../OPERATOR-WORKFLOW.md)).
- Competitor crawl is **required** after SERP import for research-ready runs ([ADR 012](012-operator-research-model.md)).
- Target-site crawl, comparison, and `gap_topics` are **wired** on the operator path ([RESEARCH-MODEL.md](../RESEARCH-MODEL.md)).
- Content Writer reads keyword + site export ([INTEGRATIONS.md](../INTEGRATIONS.md)).

## Decision

Parallel competitor module; do not refactor `PageFetchService` or Filter.

### Trigger

`POST /runs/{keywordProjectId}/competitor-crawl` — explicit via Web UI or Api. Transactional: `competitorSaved` true only after commit.

### Seed URL (operator path)

Per domain: organic row with best path/keyword match, then **lowest `RankAbsolute`** (`SeedRankAbsolute` on `competitor_pages`).

### Crawl (operator path)

**Single seed page per domain** — fetch, extract headings/meta/JSON-LD. No BFS expansion on operator path.

### Crawl (legacy pipeline)

BFS same domain; `projects.max_crawl_depth` / `max_crawl_pages` per domain; **robots.txt** before fetch.

### Extract

Structural only (headings, meta, JSON-LD, canonical). **No** body text. Legacy target pipeline may store content blocks; competitor tables do not.

### Gates (legacy pipeline)

≥ 1 page per run; ≥ 5 per domain when depth allows.

Operator **research-ready** gates: see [INTEGRATIONS.md](../INTEGRATIONS.md#research-ready-gates).

### Real-time progress (mandatory)

**Do not poll.** See [011-no-rest-polling.md](011-no-rest-polling.md).

- Push: SSE `GET /runs/{id}/competitor-crawl/progress-stream` via Channels → Postgres NOTIFY → LISTEN → `CrawlProgressBroadcaster`
- Reconnect: `GET /runs/{id}/competitor-crawl/status` once (not a loop)
- Job state on `analysis_runs` (`CompetitorCrawlStatus`, `CompetitorCrawlMessage`, timestamps)

## References

- [007-signalr-channel.md](007-signalr-channel.md)
- [011-no-rest-polling.md](011-no-rest-polling.md)
- [012-operator-research-model.md](012-operator-research-model.md)
- [plans/frase-phase.md](../plans/frase-phase.md)
- [PLAN.md](../PLAN.md)
