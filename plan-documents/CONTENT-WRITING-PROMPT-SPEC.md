# Content Writing Prompt Spec

**Source:** User prompt pasted **Saturday, Jun 13, 2026** in the Content Writing agent session.  
**Canonical rules:** [`content-writing-prompt.md`](./content-writing-prompt.md) — includes the **5 closing FAQs** requirement.  
**Related:** `plan-documents/CONTENT-WRITER-FEATURES.md` (competitive reverse-engineering).

---

## Closing FAQ rule

**Yes — every article must end with exactly 5 topic FAQs.**

This is documented in `content-writing-prompt.md` and enforced in code:

- `ContentBrief.closingFaqQuestions` — always 5 questions (PAA → gap topics → fallbacks)
- `ArticlePromptBuilder` — outline + draft prompts require `<h2>Frequently Asked Questions</h2>` + 5 `<h3>`/`<p>` pairs
- `ArticleSchemaBuilder` — `FAQPage` JSON-LD uses those same 5 questions

---

## Original prompt (preserved)

### Lean into 4-Phase Methodology

Your structured process (**Business Objectives → Data Quality Assessment → Tech Selection → Pilot Implementation Strategy**) builds trust. Turn this methodology into a downloadable PDF lead magnet on social platforms to capture regional B2B consulting leads.

### 1. Implement strict programmatic E-E-A-T elements

- **Code Snapshot Injector:** Sanitized code blocks, webhook configs, Zapier routing logic in text.
- **Localized Anchor Nodes:** South Florida / Palm Beach County compliance, state filing, logistics hubs.

### 2. Optimize for GEO

- **Direct Answer Blocks** at the top — definitions, pricing, integration steps.
- **Schema:** Auto-inject `TechArticle` / `BlogPosting` JSON-LD.

### 3. Managed approval gate

- Draft-only CMS publish; 5-minute human polish before go-live.

### Live-SERP pipeline

```
[Live SERP Scrape (PAA)] → [Competitor Schema & Gap Map] → [Model Execution]
```

### Competitive scraping

- Competitor H1–H4 outlines → gap subtopics for generation.
- Competitor schema entity extraction.

### TechArticle JSON-LD + GTM checklist

See original session transcript (Jun 13, 2026) for full manual template and GTM verification steps.

---

## Implementation status

| Requirement | Status | Where |
|-------------|--------|-------|
| Four Phase Methodology | **Done** | `WritingMethodologySpec`, `ArticlePromptBuilder` |
| Direct answer blocks (GEO) | **Done** | `DirectAnswerBlockSpec` |
| **5 FAQs at end of article** | **Done** | `content-writing-prompt.md`, `ContentWritingRules`, `ArticlePromptBuilder` |
| `FAQPage` JSON-LD (5 pairs) | **Done** | `ArticleSchemaBuilder` + `ClosingFaqQuestions` |
| E-E-A-T technical evidence | **Partial** | Prompt rules; no auto code injector |
| Geo anchor nodes | **Done** | `GeoAnchorNodes` |
| `TechArticle` JSON-LD | **Done** | `ArticleSchemaBuilder` |
| Niche + competitor enrichment | **Done** | `ContentBriefService` |
| Code Snapshot Injector | **Not built** | |
| PDF lead magnet | **Not built** | |
| WordPress auto-publish | **Deferred** | |
| GTM checklist | **Doc only** | |

---

## Code map

```
GenerateBriefRequest
  └─ ContentBriefService
       ├─ SERP PAA + niche gaps
       ├─ ContentWritingRules.BuildClosingFaqQuestions() → 5 questions
       └─ ContentBrief.closingFaqQuestions

WritingOutlineRequest / WritingDraftRequest
  └─ ArticlePromptBuilder (5-FAQ closing section in system + user prompts)

RenderedArticleResult
  └─ ArticleSchemaBuilder → FAQPage from closingFaqQuestions
```

---

*Last updated: 2026-06-17*
