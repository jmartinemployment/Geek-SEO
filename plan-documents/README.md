# Geek SEO — Planning

## Start here

| Document | Purpose |
|----------|---------|
| **[`TODO.md`](TODO.md)** | **All remaining work** (scoring v2, #12b, waivers, integrations, REDESIGN, ops, upgrade, security) |
| **[`PROJECT_STATUS.md`](../PROJECT_STATUS.md)** | What’s live in production; v1 parity #1–27 status |
| **[`docs/ROADMAP.md`](../docs/ROADMAP.md)** | One-screen index |
| **[`ARCHITECTURE.md`](ARCHITECTURE.md)** | Services, ports, API surface |

## Upgrade plans (post-v1 — not in master plan closure)

**Convention:** `UPGRADE-{competitive-target}-{themes}.md` — one file per major upgrade track. Listed in [`TODO.md`](TODO.md); does **not** block v1 “plan complete.”

| File | Upgrades from → to |
|------|-------------------|
| [`UPGRADE-se-ranking-agency-serpapi.md`](UPGRADE-se-ranking-agency-serpapi.md) | v1 content parity → SE Ranking–class rank/audit/reports + agency white-label + SerpApi (`U1`–`U10`) |

Add new files as `UPGRADE-<target>-<themes>.md` (e.g. `UPGRADE-ahrefs-backlinks.md`) and link them here + in `TODO.md`.

## Deep dives (reference only)

| Document | Purpose |
|----------|---------|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Services, ports, data flow |
| [`PLATFORM-DECOUPLING.md`](PLATFORM-DECOUPLING.md) | M0–M9 (complete) |
| Scoring (code) | `GeekSeo.Application/Services/ContentScoringService.cs` |
| [`competitor-analysis.md`](competitor-analysis.md) | Competitor research |
| [`DATAFORSEO-REPLACEMENT-UPGRADE.md`](DATAFORSEO-REPLACEMENT-UPGRADE.md) | DataForSEO expansion / SerpApi fallback |

## Do not use for planning

[`REDESIGN-PLAN.md`](REDESIGN-PLAN.md), `PARITY-SPECS.md`, `API-CONTRACTS.md`
