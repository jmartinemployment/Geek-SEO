---
name: leonardo-featured-image
description: >-
  Generate featured/hero images for Geek SEO technical articles via Leonardo.ai
  MCP. Use when the user asks for a featured image, hero image, OG image, article
  thumbnail, or Leonardo image generation for Content Writing articles.
metadata:
  author: geek-seo
  version: "1.0.0"
---

# Leonardo featured images (Geek SEO)

Generate **reader-facing hero images** for technical SEO articles. Uses the **Leonardo.ai MCP** server (not the slow web UI).

## Prerequisites

1. **Leonardo MCP** in `~/.cursor/mcp.json` (or project `.cursor/mcp.json`):

```json
"leonardo-ai": {
  "url": "https://mcp.leonardo.ai/v1/mcp",
  "headers": {
    "API-Key": "YOUR_LEONARDO_API_KEY"
  }
}
```

2. Cursor **Agent mode** (MCP tools are not available in Ask mode).
3. Confirm green status: **Cursor Settings → MCP → leonardo-ai**.

API key: [Leonardo — Create your API key](https://docs.leonardo.ai/docs/create-your-api-key)

## When to use

- User wants a **featured / hero / OG image** for a Content Writing article
- User is experimenting with image style before we wire backend automation
- Article is **technical** (bookkeeping automation, integrations, SaaS tooling, etc.)

Do **not** use for in-article screenshots, diagrams with labels, or images that must contain readable text — Leonardo is weak at text; use Ideogram only when text is essential.

## Workflow

### 1. Gather context from the article

From the document or user message, collect:

| Field | Source |
|-------|--------|
| Primary keyword | `content_document.keyword` or article title |
| Topic one-liner | H1 or first paragraph |
| Tone | Professional, modern B2B; avoid stock-photo clichés |
| Avoid | Logos, brand names, faces unless requested, watermarks, text overlays |

### 2. Build the image prompt

Use this template and fill in bracketed parts:

```
Professional editorial hero image for a technical business article about [TOPIC].
Scene: [CONCRETE VISUAL METAPHOR — e.g. clean desk with laptop showing abstract dashboard charts, subtle automation flow lines, organized documents].
Style: modern flat illustration with soft gradients, cool blue and slate palette, minimal, no clutter.
Lighting: bright, trustworthy, corporate but not sterile.
Composition: wide horizontal banner, subject weighted left or center, negative space on right for optional title overlay later.
Constraints: no text, no letters, no logos, no watermarks, no human faces unless user asked.
```

**Visual metaphor ideas by topic:**

| Topic area | Metaphor |
|------------|----------|
| Bookkeeping / accounting automation | Ledger grid morphing into digital nodes, receipt stack + checkmarks |
| Integrations (Zapier, QuickBooks) | Connected app tiles as abstract shapes, sync arrows |
| Data quality | Funnel filtering clean vs messy data blocks |
| Pilot / rollout | Stepped roadmap, small team silhouette from behind (optional) |

Keep prompts **under ~400 characters** for the MCP tool; put detail in style/constraints, not keyword stuffing.

### 3. Call Leonardo MCP

Use the **`generate-image`** tool from the `leonardo-ai` MCP server.

**Defaults for featured images:**

| Setting | Value | Why |
|---------|-------|-----|
| Aspect | **16:9** or **1200×675** if dimensions are explicit | Blog hero + social OG crop |
| Model | **Lucid Origin** | General editorial / illustration |
| Alternative | **Lucid Realism** | Photoreal office/product shots |
| Text in image | **Ideogram 3.0** only if user insists on text in the image |

If the tool accepts `guidance` / `prompt`, pass the built prompt verbatim.

Poll or wait for completion per tool response; save the returned image URL.

### 4. Deliver to the user

1. Show the image URL(s) from Leonardo.
2. Suggest download path for local review: `frontend/public/article-images/` or user Downloads.
3. Note: **Geek SEO does not yet persist `featured_image_url` on `content_document`** — this is manual/experimental until backend wiring ships.

### 5. Iterate

If the user dislikes the result, change **one** dimension at a time:

- metaphor (desk → abstract nodes)
- palette (cool blue → warm neutral)
- model (Origin → Realism)
- stronger "no text" constraint

## Example prompts (ready to use)

**Bookkeeping automation:**

```
Professional editorial hero for technical article on automated bookkeeping.
Abstract: paper receipts transforming into organized digital ledger tiles on a laptop screen, subtle green check accents.
Modern flat illustration, slate and teal, wide 16:9, clean negative space right third.
No text, no logos, no faces.
```

**Integration guide:**

```
Hero image for B2B software integration article.
Connected abstract app blocks linked by glowing lines, cloud sync motif, minimal isometric style.
Cool gray and blue palette, wide banner composition, no text or brand marks.
```

## Related skills

| Skill | Use |
|-------|-----|
| `.agents/skills/image-gen` | Generic image gen via Max/Gemini API (`MAX_API_KEY`) — different stack |
| This skill | Leonardo MCP + Geek SEO article hero conventions |

## Future backend (not implemented)

When automating in `GeekSeoBackend`:

1. After draft completes, build prompt from `WritingResearchContext` or `ContentBrief`.
2. `POST` Leonardo REST API (or queue job), upload to Vercel Blob / S3.
3. Set `featured_image_url` on `content_document` via GeekRepository.

Until then, MCP + this skill is the supported experimentation path.

## References

- [Leonardo MCP setup](https://docs.leonardo.ai/docs/connect-to-leonardoai-mcp)
- [Leonardo API](https://docs.leonardo.ai/)
