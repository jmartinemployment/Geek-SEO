# Product phases

Site Analyzer is **not** Semrush, Surfer, or MarketMuse. It is a **phased** research stack for Geek-SEO.

## North star (long-term)

Help **client sites rank better** for pillar keywords — but each phase delivers a **different slice** of that loop. Do not build later phases while the current phase is sloppy.

## Phases

| Phase | Analog (conversation only) | Job | Return |
|-------|------------------------------|-----|--------|
| **1 — Frase** | Frase | SERP → **research pack** → Content Writer draft | Writer ships a pillar page without tab hoarding |
| **2 — Rankings loop** (now) | (none — our differentiator) | Re-import SERP → position delta for pillar + target URL | Proof the page moved (or didn’t) |
| **3 — Surfer** | Surfer | On-page optimization for **one** URL vs SERP | Tune draft/page before publish |
| **4 — MarketMuse** | MarketMuse | Site-wide topical authority / clusters | Strategy across many pillars |

**Frase does not promise rankings.** Rankings loop does not replace Frase — it closes the loop **after** publish.

**Content quality bar:** Does the pillar page fully answer the query **better than the seed pages** — with citations/named sources where it matters and prudent human oversight of AI drafts? See [plans/frase-phase.md](plans/frase-phase.md).

## Frase phase boundary

**In scope (Site Analyzer)**

- SERP HTML import + relevance filter (included / rejected / excluded)
- Competitor crawl: **one seed page per domain**
- Target-site fetch + extract for Project URL
- Comparison → `gap_topics`, `writing_instructions`
- Research-ready gates + Content Writer handoff (`analysisRunId` → read `sa2`)

**Owned by Content Writer (not SA2)**

- Article outline, section structure, closing FAQs in draft
- Content score, Sources, Citations, meta improvements, JSON-LD

**Out of scope until later phases**

- Domain Overview / portfolio keyword index (Semrush-shaped)
- GSC integration, competitor rank history, portfolio rank charts
- NLP content scores in Site Analyzer (Surfer lives in Writer today)
- Site-wide content silos (MarketMuse)
- Site Audit (`site_audit_runs`) — separate track

## Canonical implementation plans

| Phase | Plan |
|-------|------|
| 1 — Frase | [plans/frase-phase.md](plans/frase-phase.md) |
| 2 — Rankings loop | [plans/rankings-loop.md](plans/rankings-loop.md) |

## Operator docs

- [OPERATOR-WORKFLOW.md](OPERATOR-WORKFLOW.md) — steps on Web UI
- [RESEARCH-MODEL.md](RESEARCH-MODEL.md) — fields and glossary
- [HANDOFF.md](HANDOFF.md) — Content Writer
- [INTEGRATIONS.md](INTEGRATIONS.md) — export contract and gates

## ADR

- [decisions/014-product-phases-frase-first.md](decisions/014-product-phases-frase-first.md)
