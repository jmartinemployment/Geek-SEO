# Content Writing Prompt

Canonical product rules for Geek SEO article generation. Implemented in `ArticlePromptBuilder`, `ContentBriefService`, and `ArticleSchemaBuilder`.

**Companion:** `plan-documents/CONTENT-WRITING-PROMPT-SPEC.md` (original strategy + implementation tracker).

---

## Required article structure

Every generated article must follow this shape:

1. **H1** — article title (once)
2. **Direct answer block** — concise definition + business outcome in the first paragraphs (GEO)
3. **Body** — methodology phases, technical detail, competitor-informed subtopics
4. **Closing FAQ section (required)** — see below

---

## Closing FAQ section (required)

**Every article must end with exactly 5 topic FAQs.**

| Rule | Detail |
|------|--------|
| **Count** | Exactly **5** questions and answers |
| **Placement** | Last major section of the article (after body content) |
| **Heading** | `<h2>Frequently Asked Questions</h2>` |
| **Format** | Each question = `<h3>`; each answer = `<p>` (2–4 sentences, factual, concise) |
| **Question source** | SERP People Also Ask first → niche gap topics → keyword fallbacks until 5 |
| **Schema** | Same 5 Q&A pairs feed `FAQPage` JSON-LD on render |

### Example closing structure

```html
<h2>Frequently Asked Questions</h2>
<h3>What is Zapier QuickBooks integration?</h3>
<p>…</p>
<h3>How much does Zapier QuickBooks integration cost?</h3>
<p>…</p>
<!-- 3 more Q&A pairs -->
```

### Prompt text sent to the model

Outline system prompt:

> Always end with `<h2>Frequently Asked Questions</h2>` and exactly 5 `<h3>` FAQ questions (no answers in the outline).

Draft system prompt:

> Always close with `<h2>Frequently Asked Questions</h2>` containing exactly 5 topic FAQs as `<h3>` + `<p>` pairs.

Draft user prompt includes the numbered list of 5 questions from `ContentBrief.closingFaqQuestions`.

---

## Four Phase Methodology

Structure body content around:

1. Business Objectives
2. Data Quality Assessment
3. Tech Selection
4. Pilot Implementation Strategy

---

## E-E-A-T (technical B2B)

- Include sanitized code, webhook, or Zapier routing examples when the topic is technical
- Reference software versions and implementation assumptions where they matter
- Anchor to localized business context (service area, compliance, logistics) via geo anchor nodes

---

## GEO (Generative Engine Optimization)

- Open with direct-answer blocks — no conversational fluff at the top
- Use punchy definitions, pricing expectations, or integration steps
- Mirror PAA phrasing in headings where natural

---

## Schema targets

- Primary: `TechArticle` JSON-LD
- Additional: `FAQPage` (the 5 closing FAQs)
- Software entities: `SoftwareApplication` nodes for detected stacks (QuickBooks, Zapier, HubSpot, etc.)

---

## Pre-publish review checklist

- Verify software versions and code logic
- Confirm direct-answer blocks are factual and concise
- **Confirm the closing FAQ section contains exactly 5 answered questions**
- Check local references against the target service area

---

## Code references

| Constant / field | Location |
|------------------|----------|
| `ClosingFaqCount = 5` | `ContentWritingRules.cs` |
| `ClosingFaqQuestions` | `ContentBrief` model |
| Prompt assembly | `ArticlePromptBuilder.cs` |
| FAQ question builder | `ContentWritingRules.BuildClosingFaqQuestions()` |
| FAQPage JSON-LD | `ArticleSchemaBuilder.cs` |

---

*Last updated: 2026-06-17*
