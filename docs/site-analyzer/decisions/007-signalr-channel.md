# ADR 007: SignalR Channel

## Status
Accepted

## Hub
`RunProgressHub` at `/hubs/run-progress`

## Methods (server → client)
- `StageCompleted(runId, stage, passed, validationMessage, status)`

Competitor crawl progress uses **SSE** (`GET /runs/{id}/competitor-crawl/progress-stream`), not this hub. Hub remains for legacy SERP pipeline events.

Competitor crawl server chain: `System.Threading.Channels` → Postgres `NOTIFY sa2_competitor_crawl_progress` → `LISTEN` → `CrawlProgressBroadcaster` → SSE.

## Client → server
- `SubscribeToRun(runId)` — joins group `run-{runId}`
- `UnsubscribeFromRun(runId)`

Hub observes orchestration only; does not drive stages.
