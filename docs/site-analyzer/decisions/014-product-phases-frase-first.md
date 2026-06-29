# ADR 014: Product phases — Frase first

## Status

Accepted (amended — Writer-centric research pack)

## Context

Build drifted toward Semrush-shaped UI (Domain Overview, portfolio metrics) while the operator goal was **rankings for pillar keywords**. Free Semrush tools and clone features did not deliver measurable return.

The team agreed to phase the product:

1. **Frase** — research pack → Content Writer draft (now)
2. **Rankings loop** — re-import SERP, position delta
3. **Surfer** — on-page optimization for one URL
4. **MarketMuse** — site-wide topical strategy

Content Writer at `seo.geekatyourspot.com` already provides Insights, internal outlines, scoring, Sources/Citations, and JSON-LD. Site Analyzer must **supply** ranking-oriented research, not duplicate Writer features.

## Decision

- **Phase 1 is Frase.** All operator work must pass: “Does this improve SERP → **research pack** → Content Writer?”
- **Do not** expand Semrush analogs (Domain Overview index, Keyword Gap, portfolio dashboards) during Frase phase.
- **Site Analyzer does not build article outlines.** Geek-SEO `ArticlePromptBuilder` derives outline, section hints, and closing FAQs from `sa2` research loaded by `analysisRunId` via `ContentWriterSerpExportMapper`.
- **`gap_topics`** are research-pack fodder; operator-facing label is **gap themes** / **writing brief**, not “SEO gap analysis” or a second outline editor.
- **Competitor crawl** stays seed-only (one ranking URL per domain); no whole-site BFS.
- **Rankings loop** is the next phase after pack quality and handoff are production-solid.
- **Docs:** Mark wiring status only with code-path + verification evidence ([plans/frase-phase.md](../plans/frase-phase.md#documentation-rule-evidence-before-status)).

## Consequences

- Implementation plan: [plans/frase-phase.md](../plans/frase-phase.md)
- Product map: [PRODUCT-PHASES.md](../PRODUCT-PHASES.md)
- Gate resolution: [INTEGRATIONS.md](../INTEGRATIONS.md#research-ready-gates)
- Handoff: [HANDOFF.md](../HANDOFF.md)
- Deferred: [plans/site-audit-and-competitive-research.md](../plans/site-audit-and-competitive-research.md), Semrush Keyword Overview UI parity

## References

- [RESEARCH-MODEL.md](../RESEARCH-MODEL.md)
- [012-operator-research-model.md](012-operator-research-model.md)
