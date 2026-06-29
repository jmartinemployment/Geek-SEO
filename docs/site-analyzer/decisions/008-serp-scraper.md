# ADR 008: Self-Built Google SERP Scraper

## Status
Accepted — supersedes ADR 002 (paid SERP API adapters)

## Production path (2026 — operator import)

**Railway production uses `SERP_EXECUTION=manual`.** The operator searches Google in Chrome, saves **Webpage, HTML only**, and imports via `scripts/import-serp-html.sh`. The Api **does not** scrape Google in production.

Automated `GoogleScraperProvider` / Mac `SiteAnalyzer2.SerpWorker` remain in the repo for **local dev, tests, and legacy** paths — not the current production workflow. See [OPERATOR-WORKFLOW.md](../OPERATOR-WORKFLOW.md).

## Decision (design)
SiteAnalyzer owns Google organic-results discovery via `GoogleScraperProvider` (`ProviderKey = "google-scraper"`). No third-party SERP API (SerpAPI, DataForSEO, Serper.dev, or equivalent) in any environment. `fixture` remains dev/test-only.

## Usage volume (governs risk posture)
One client project at a time; ~one keyword lookup per article; on the order of a dozen lookups per project. Design for human-paced, low-frequency use — not industrial proxy-scale volume.

## Interface (unchanged)
```csharp
public interface ISerpProvider {
    string ProviderKey { get; }
    Task<SerpResultSet> FetchOrganicResultsAsync(SerpQuery query, CancellationToken ct);
}
```

## Pacing defaults
| Setting | Default | Notes |
|---------|---------|-------|
| `GOOGLE_SCRAPE_MIN_DELAY_MS` | `5000` | Hard floor between outbound Google requests |
| Jitter band | `min .. min+3000` ms | Randomized gap; never a fixed metronome |
| Rolling window | 30 minutes | Per scraper instance timestamp ring |

## Adaptive early warning (advisory, not a gate)
If ≥3 requests in the rolling window AND average inter-request gap < `min + 1500` ms:
- Annotate SERP `validation_message` with pacing warning copy
- Next delay = jittered minimum × 1.5 (cap 30s)

Warning copy: `"SERP pacing warning: recent requests clustered faster than human browsing rhythm; next delay lengthened proactively."`

## IP / transport
- **No proxy** in v1
- **Production (Railway):** `SERP_EXECUTION=manual` — operator uploads Chrome-saved HTML; Api does not call Google. See production addendum at top of this ADR.
- **Legacy production path:** `SERP_EXECUTION=external` + Mac `SiteAnalyzer2.SerpWorker` (home-network scrape, post back to Api). Deprecated in favor of manual import; still in repo.
- **Local dev / CI:** `SERP_EXECUTION=inline` or `manual` with HTML upload; automated `GoogleScraperProvider` optional on Mac; integration tests use `fixture`.
- Raw HTTP GET first (automated paths); **Playwright escalation when raw HTTP returns Google's JavaScript shell**
- Env `GOOGLE_SCRAPE_PLAYWRIGHT`: `auto` (default), `always`, `never`
- Realistic rotating User-Agent pool (small maintained set)

## External worker (legacy — not current Railway production)
| Api env | Purpose |
|---------|---------|
| `SERP_EXECUTION=external` | Skip inline Serp in `StartRunAsync`; worker completes stage |
| `SERP_WORKER_API_KEY` | Bearer token for `/internal/runs/*` endpoints |
| `SERP_WORKER_CLAIM_TIMEOUT_SECONDS` | Fail unclaimed runs (default **90**) |

Worker env (Mac, manual start): `SERP_WORKER_API_URL`, `SERP_WORKER_API_KEY`, optional `SERP_WORKER_POLL_INTERVAL_SECONDS` (default **15**).

Internal endpoints: `GET /internal/runs/pending-serp`, `POST .../claim`, `POST .../worker-result`. Api owns gate writes + SignalR (`SerpClaimed`, `StageCompleted`). Web UI is SignalR-only — no Serp polling.

## Organic result definition
Included: standard organic listings. Excluded by default: paid ads, shopping carousels, local pack/map, featured snippets (unless a future ADR adds them as separate signal types).

## PAA
Parse People Also Ask to `serp_paa_results` (`run_id`, `question_text`, `sequence`). Surface organic + PAA counts in `validation_message` (e.g. `"14 organic results, 3 PAA questions found."` or `"14 organic results, 0 PAA questions found."`).

## Sanity check (failure, not zero-results pass)
If parsed organic count = 0 OR no url/title pairs extractable → stage fails: `"SERP fetch returned no parseable organic results — likely blocked or layout changed. Stage failed."`

## Failure handling
| Condition | Behavior |
|-----------|----------|
| CAPTCHA detected | Fail immediately; message identifies CAPTCHA |
| HTTP 403/429 or 200 non-SERP body | Fail with status/signature in message |
| Partial field parse on a row | Drop that row; gate still applies to valid rows |
| Scrape failure mid-run | Run fails; no in-run retry/degrade |

## Resolver
`SerpProviderResolver`: live = `google-scraper` only; dev/test = `fixture`. Remove `serpapi` / `dataforseo` keys entirely (no silent fallback to fixture for unknown keys in production).

## Future multi-engine note
Additional `ISerpProvider` implementations (`bing-scraper`, etc.) and engine rotation sit above adapters; `serp_results` should remain source-agnostic via `serp_provider_key` on the run row.
