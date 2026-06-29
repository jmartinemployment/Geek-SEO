# Content Writer — Marketing handoff

How pillar articles in **Geek-SEO** become publishable assets on **geekatyourspot.com**.

**Spec:** [`plan-documents/CONTENT-WRITER-MARKETING-EXPORT.md`](../plan-documents/CONTENT-WRITER-MARKETING-EXPORT.md)  
**Pillar article rules:** [`plan-documents/content-writing-prompt.md`](../plan-documents/content-writing-prompt.md)  
**geekatyourspot paste targets:** `geekatyourspot-r/docs/content-writer-brief.md`

---

## Where content lives

| Asset | Geek-SEO storage | UI |
|-------|------------------|-----|
| Pillar article | `seo_content_documents.content_html` | Content editor (main pane) |
| Catalog summaries + blog + social | `seo_content_documents.marketing_bundle_json` | Marketing export panel (right rail) |

The `content-output/` directory is **not** used by the SaaS app. Bundles persist in Postgres per document.

---

## Workflow (pillar-first)

1. Open `/content-writing` — write or generate the **pillar** from Site Analyzer handoff.
2. In **Marketing export** (right rail): set **department** and **use case slug**.
3. **Generate from pillar** → `homeSummary`, `hubSummary`, `metaDescription`.
4. **Generate spoke** → blog post (pick spoke type; optional distinct keyword).
5. **Generate social** → LinkedIn + Facebook drafts.
6. **Save bundle** after edits.
7. Validation panel must pass before paste to geekatyourspot-r.
8. Copy fields manually into geekatyourspot-r TypeScript catalogs (see paste map below).

---

## Marketing bundle JSON shape

```json
{
  "departmentSlug": "marketing",
  "useCaseSlug": "content-operations",
  "primaryKeyword": "AI content operations",
  "homeSummary": "...",
  "hubSummary": "...",
  "metaDescription": "...",
  "blogSpoke": {
    "slug": "what-ai-content-tooling-costs",
    "primaryKeyword": "what AI content tooling actually costs",
    "spokeType": "cost",
    "title": "...",
    "contentHtml": "...",
    "excerpt": "...",
    "metaDescription": "..."
  },
  "social": {
    "linkedin": { "body": "...", "linkTargetKind": "pillar", "linkTargetSlug": "content-operations" },
    "facebook": { "body": "...", "linkTargetKind": "blog", "linkTargetSlug": "what-ai-content-tooling-costs" }
  }
}
```

---

## Paste map (Geek-SEO → geekatyourspot-r)

| Bundle field | geekatyourspot-r destination |
|--------------|------------------------------|
| Pillar `contentHtml` | `data/use-cases/{dept}/content/{slug}.ts` → `html` |
| `homeSummary` | `data/use-cases/{dept}/catalog.ts` |
| `hubSummary` | `data/use-cases/{dept}/catalog.ts` |
| `metaDescription` | `data/use-cases/{dept}/catalog.ts` |
| `primaryKeyword` | `data/use-cases/{dept}/catalog.ts` |
| `blogSpoke.contentHtml` | `data/blog/posts/{slug}.ts` → `html` |
| `blogSpoke.excerpt` | `data/blog/catalog.ts` → `excerpt` |
| `blogSpoke.metaDescription` | `data/blog/catalog.ts` |
| `blogSpoke.primaryKeyword` | `data/blog/catalog.ts` |
| `social.linkedin.body` | `data/social/{slug}.ts` |
| `social.facebook.body` | `data/social/{slug}.ts` |

After paste: register blog in `lib/blog/index.ts` and `data/blog/static-posts.ts`; register social in `lib/social/index.ts`.

---

## API (GeekSeoBackend)

| Method | Path |
|--------|------|
| GET | `/api/seo/content/{id}/marketing-bundle` |
| PUT | `/api/seo/content/{id}/marketing-bundle` |
| POST | `/api/seo/content/{id}/marketing-bundle/validate` |
| POST | `/api/seo/content/{id}/marketing-bundle/generate-summaries` |
| POST | `/api/seo/content/{id}/marketing-bundle/generate-blog-spoke` |
| POST | `/api/seo/content/{id}/marketing-bundle/generate-social` |

Persistence: `PATCH api/seo/internal/content/{id}/marketing-bundle` on GeekRepository.

---

## Deploy checklist

1. Apply EF migration `AddMarketingBundleToContentDocuments` (`MarketingBundleJson` column).
2. Deploy **GeekRepository** with `UpdateMarketingBundleAsync` endpoint (GeekBackend repo).
3. Deploy **GeekSeoBackend** + frontend.

---

## Validation rules (Geek-SEO)

- `homeSummary`, `hubSummary`, `metaDescription` — all required, pairwise distinct.
- Blog `primaryKeyword` — must not exact-match or substring-collide with pillar keyword (stopword-normalized).
- LinkedIn and Facebook bodies — must be different strings.

geekatyourspot-r build gates catch additional paste mistakes at `next build`.

---

*Last updated: 2026-06-29*
