# Implementation plans

Active work: **[Rankings loop](plans/rankings-loop.md)** — re-import SERP → target domain position delta.

Frase phase (research pack → Content Writer): **[frase-phase.md](plans/frase-phase.md)** — shipped; E2E verification optional.

Product framing: **[PRODUCT-PHASES.md](PRODUCT-PHASES.md)** · ADR **[014-product-phases-frase-first.md](decisions/014-product-phases-frase-first.md)**

## Phase map

| Phase | Focus | Plan |
|-------|--------|------|
| **1 — Frase** | Research in `sa2` → `analysisRunId` handoff | [plans/frase-phase.md](plans/frase-phase.md) |
| **2 — Rankings loop** (now) | Re-import SERP, position delta | [plans/rankings-loop.md](plans/rankings-loop.md) |
| **3 — Surfer** | On-page optimization vs SERP | Not started |
| **4 — MarketMuse** | Site-wide topical clusters | Not started |

## Operator docs

| Doc | Purpose |
|-----|---------|
| [PRODUCT-PHASES.md](PRODUCT-PHASES.md) | What we are / are not building |
| [RESEARCH-MODEL.md](RESEARCH-MODEL.md) | Fields, glossary, wiring status |
| [INTEGRATIONS.md](INTEGRATIONS.md) | `sa2` field mapping, gate resolution, legacy export notes |
| [OPERATOR-WORKFLOW.md](OPERATOR-WORKFLOW.md) | Web UI steps |
| [HANDOFF.md](HANDOFF.md) | Content Writer |

## Deferred (do not expand during Frase)

| Doc | Was |
|-----|-----|
| [plans/site-audit-and-competitive-research.md](plans/site-audit-and-competitive-research.md) | Semrush Site Audit / Keyword Gap / Domain Overview |
| [plans/keyword-overview.md](plans/keyword-overview.md) | Semrush Keyword Overview UI parity |

## Shipped foundation (Frase inputs)

- SERP import + relevance filter + rejected rows
- Competitor **seed-only** crawl ([010](decisions/010-competitor-crawl-planned.md))
- SSE crawl progress ([011](decisions/011-no-rest-polling.md))
- Operator research assembly ([012](decisions/012-operator-research-model.md)) — target crawl, comparison, `gap_topics`

## Superseded active plan

[plans/fix-site-analyzer-research.md](plans/fix-site-analyzer-research.md) — operator wiring **shipped**; remaining Frase work = pack quality, UI, docs (see **frase-phase.md**).

## Historical

- [archive/SiteAnalyzer-PLAN.md](archive/SiteAnalyzer-PLAN.md) — legacy step-gated pipeline
