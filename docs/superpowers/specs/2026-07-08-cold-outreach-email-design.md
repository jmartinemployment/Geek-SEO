# Cold Outreach Email Output — Design Spec

**Date:** 2026-07-08  
**Status:** Approved for planning  
**Scope:** Add Cold Outreach as a generated email output; stub other email types for later. Also remove the Geek-SEO app sidebar on seo.geekatyourspot.com (including `/content-writer`).

## Goal

Add a **Cold Outreach / Sales** email as a fifth content output in Content Writer: subject line + plain-text body (50–125 words) + one clear CTA that always links to the pillar article URL. Unlock after the pillar body (same gate as social). Place its generate button last in the UI. Stub Newsletter, Story-Based/Nurture, and Transactional email types for future work.

## Context

Today the pipeline produces:

1. Pillar plan (Technical Article)
2. Pillar body
3. Blog Post
4. Social (Facebook + LinkedIn)

There is no email content type. Length guidance already lives in `ContentLengthTargets` / `CONTENT_LENGTH_TARGETS` for pillar, blog, listicle, and news. Email length guidance from product:

| Email type | Length | Purpose |
|---|---|---|
| Cold Outreach / Sales | 50–125 words | High response; single clear CTA |
| Newsletters (Curated) | 200–400 words | Summarize links; drive traffic |
| Story-Based / Nurture | 500–1,000 words | Deep trust; exclusive-blog feel |
| Transactional | Under 50 words | Critical data; zero fluff |

## Decisions (locked)

| Decision | Choice |
|---|---|
| First email type | Cold Outreach only (others stubbed) |
| Unlock gate | After pillar body (same as social) |
| Button order | Last in the Generate Content step list |
| Output shape | Subject + plain-text body + CTA |
| CTA destination | Always pillar article URL |
| Implementation approach | Mirror social: new `GeneratedContentType`, one LLM call, reuse `GeneratedContent` row |

## Approach

**Mirror social generation** rather than introducing a dedicated email entity or bundling into the social step.

- Smallest change that matches existing orchestrator/API/UI patterns.
- Keep email as its own step and endpoint so “Generate all” and stubs stay clear.
- Stretch `GeneratedContent` slightly for email fields (acceptable for v1; can extract later if other email types need richer structure).

## Storage shape

### Domain

Extend `GeneratedContentType`:

| Value | Name | Status |
|---|---|---|
| 4 | `EmailColdOutreach` | Implemented |
| 5 | `EmailNewsletter` | Stub (enum + length targets only) |
| 6 | `EmailStoryNurture` | Stub |
| 7 | `EmailTransactional` | Stub |

Persist cold outreach on existing `GeneratedContent`:

| Field | Usage |
|---|---|
| `ContentType` | `EmailColdOutreach` |
| `Title` | Subject line |
| `BodyHtml` | Plain-text body only (no HTML); word count is measured on this field |
| `MetaDescription` | CTA label (reuse existing nullable string; no new column) |
| `RelatedArticleUrl` | Pillar article URL (CTA destination) |
| `Slug` | `{articleSlug}-cold-outreach` |
| `WordCount` | Body word count (existing machinery) |

No JSON+LD for email (`JsonLdSchema` remains null).

### LLM JSON contract

Mirror social’s JSON parse path. Model returns:

```json
{ "subject": "...", "bodyText": "...", "ctaLabel": "..." }
```

Orchestrator validates body word count (50–125), ensures `ctaLabel` is non-empty, then persists with `RelatedArticleUrl` set to the pillar URL (model must not invent a different destination).

### Frontend / API DTO

`GeneratedContentSet` gains:

```ts
coldOutreachEmail: {
  subject: string;
  bodyText: string;
  ctaLabel: string;
  ctaUrl: string;
} | null;
```

Assembler maps: `Title` → subject, `BodyHtml` → bodyText, `MetaDescription` → ctaLabel, `RelatedArticleUrl` → ctaUrl.

## Generation flow

### Backend

- New orchestrator method: `GenerateColdOutreachAsync(projectId)`.
- New API: `POST /projects/{id}/generate/email-cold-outreach` (exact route naming should match existing generate controller style).
- Gate: `RequireCompletePillar` (crawled site + research + pillar body ≥ 200 words) — same as social.
- One LLM call via existing provider factory, grounded on pillar title/meta/summary (and site tone where already available for social).
- Prompt requirements (JSON contract above):
  - Subject line (short, specific)
  - Plain-text body **50–125 words**
  - Single clear CTA label
  - CTA destination is always the pillar article URL (injected by orchestrator into storage / UI; prompt may instruct the model to refer to “the article” without inventing URLs)
- On regenerate: remove existing `EmailColdOutreach` row(s), then insert the new one (same replace pattern as social).
- `GenerateAllAsync`: after social, also generate cold outreach when missing/remaining.

### Length targets

Add to `ContentLengthTargets` and frontend `CONTENT_LENGTH_TARGETS`:

| Key | Min | Max | Definition |
|---|---|---|---|
| Cold outreach | 50 | 125 | High response rates; pitch a single, clear call-to-action. |
| Newsletter (stub) | 200 | 400 | Summarize external links; drive traffic back to the website. |
| Story nurture (stub) | 500 | 1_000 | Build deep trust; treat email like an exclusive blog post. |
| Transactional (stub) | 1 | 49 | Deliver critical data; highly functional with zero fluff. |

Stubs: constants + editorial definition strings only. No prompts, endpoints, or UI.

## UI

In `ContentResults`:

1. Keep Steps 1–4 (pillar plan, pillar body, blog, social) unchanged.
2. Add **Step 5 — Cold outreach email** as the last step row:
   - Description cites 50–125 words and single CTA to the pillar.
   - Enabled when pillar body is done (independent of whether blog/social exist).
   - Regenerate label when a cold-outreach result already exists.
3. “Generate all remaining steps” includes cold outreach after social.
4. Results tabs: add **Cold Outreach** as the last tab — subject, body, CTA/link, copy controls (match social copy UX where practical).

## Errors

- Project not found / not crawled / no research → existing generation errors.
- Pillar body missing → “Generate the pillar body (Step 2) before continuing.”
- LLM / provider failures → surface via existing `ApiError` path.

## Out of scope

- Sending email or ESP integration
- HTML email templates / multipart MIME
- A/B subject lines
- Personalization tokens beyond existing crawl tone/company context
- Implementing Newsletter, Story-Based, or Transactional generation in this pass

## Testing

- If social generation already has unit/prompt tests, mirror them for cold-outreach length bounds and CTA URL presence.
- Otherwise: manual path — generate after pillar body, verify subject/body/CTA/URL, regenerate replaces prior row, tab copy works.

## Success criteria

- User can generate a cold outreach email after Step 2.
- Output is subject + 50–125 word plain body + CTA to the pillar URL.
- Step button and results tab appear last.
- Other three email types exist only as stubs (enums/length targets).
- Existing pillar/blog/social behavior unchanged.
