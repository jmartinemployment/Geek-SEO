# Site Analyzer fold-in (sa2 → Geek-SEO)

**Status:** Planned  
**Phase:** [`IMPLEMENTATION-PLAN.md`](IMPLEMENTATION-PLAN.md) § 1.1  
**Related:** [`FRASE-PARITY-ASSESSMENT.md`](FRASE-PARITY-ASSESSMENT.md)

## Goal

Move the **connected** Site Analyzer (sa2, today on Railway) into this repo so research and Content Writing share one product surface. Keep the production handoff contract: `analysisRunId` → Content Writing.

## Today

| Piece | Location |
|-------|----------|
| Site Analyzer wizard + `sa2` DB | Railway **Site Analyzer** project |
| Content Writing handoff | Geek-SEO via `HttpAnalysisRunRepository`, `analysisRunId` |
| Transitional 10-step wizard | Geek-SEO `/site-analyzer`, `SiteAnalyzerStepService` (`urlResearchId`) — **not canonical** |

## Scope

1. **Import** sa2 UI (e.g. `site-analyzer2-workspace`), `site_profiles`, `analysis_runs`, content-writer-export API into Geek-SEO frontend + backend.
2. **Persistence** — `sa2` schema via `GeekSeo.Persistence` + GeekRepository internal routes; retire separate `SITE_ANALYZER2_DATABASE_URL` cross-service HTTP where possible.
3. **Keep** `ContentWriterHandoffService` + `analysisRunId` contract unchanged.
4. **Cutover** — parallel run → redirect old SA URL → decommission Railway Site Analyzer service.
5. **Retire** after cutover: `SiteAnalyzerStepService`, transitional `/site-analyzer` 10-step UI, `seo_site_research` / `seo_url_research` wizard path (or read-only archive).

## Non-goals

- Changing Content Writing to accept `urlResearchId` again.
- Merging Niche Analyzer into Site Analyzer (separate products).

## Tests (must stay green)

- `ContentWriterHandoffService` / attach contract tests
- `analysisRunId` gate on `/content-writing`
- Research pack export used by insights rail

## Checklist

- [ ] sa2 routes and entities in Geek-SEO + GeekRepository
- [ ] Site Analyzer UI under Geek-SEO app shell
- [ ] Write article → `contentWritingPath({ analysisRunId })` unchanged
- [ ] Railway Site Analyzer decommissioned
- [ ] Transitional 10-step wizard removed
- [ ] Docs: no `urlResearchId` handoff references
