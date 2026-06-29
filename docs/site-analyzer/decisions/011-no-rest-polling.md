# ADR 011: No REST polling for long-running job progress

## Status
Accepted — supersedes any competitor-crawl polling implementation.

## Context

Competitor crawl was initially wired as `POST` + `GET` every 5 seconds. This failed in production:

1. **False UI success/failure** — client treated `crawlStatus === "complete"` as success when `competitorSaved` was false.
2. **Database load** — each poll loaded full `competitor_pages` with headings, meta, and JSON-LD.
3. **Multi-replica Railway** — in-memory job dictionaries were invisible to other API instances; later DB status helped GET but polling remained wasteful and fragile.
4. **Operator impact** — immediate generic error, empty browser console (HTTP 200 with misleading body).

## Decision

**Never use REST polling loops for long-running job progress.**

### Server

1. **Channels** — crawl workers publish to `CompetitorCrawlProgressPublisher`.
2. **PostgreSQL NOTIFY** — channel `sa2_competitor_crawl_progress` (all API replicas).
3. **LISTEN hosted service** — each replica → `CrawlProgressBroadcaster.BroadcastToRun` using `Utf8JsonReader` runId scan (no `JsonDocument`).
4. **SSE endpoint** — `GET /runs/{id}/competitor-crawl/progress-stream` (`text/event-stream`).
5. **Progress log** — `competitor_crawl_progress_logs` stores sequenced JSON payloads (`Id` = `sequenceNumber`).
6. **Catch-up endpoint** — `GET /runs/{id}/competitor-crawl/progress-catchup?lastSeq=N` returns missed events after reconnect.
7. **Status endpoint** — `GET /runs/{id}/competitor-crawl/status` for first connect when `lastSeq === 0`.

`RunProgressHub` (SignalR) remains for legacy SERP pipeline events only — not competitor crawl.

### Client

- Native **`EventSource`** via layout-level **`CrawlStreamProvider`** (`src/context/CrawlStreamContext.tsx`).
- Track `sequenceNumber` per run; on SSE `onopen` with `lastSeq > 0`, replay via **progress-catchup** before live stream resumes.
- On first connect (`lastSeq === 0`), one-shot **status** sync; `window.online` triggers catch-up for active runs.

## Consequences

- Crawl progress is event-driven; threads and DB stay idle between updates.
- Works on Railway multi-replica without Redis, using existing Postgres.
- Smaller Next.js bundle (no SignalR client for operator crawl).
- Full `GET /competitor-crawl` remains for fetching saved page payloads after completion, not for progress.

## References

- [007-signalr-channel.md](007-signalr-channel.md)
- [010-competitor-crawl-planned.md](010-competitor-crawl-planned.md)
