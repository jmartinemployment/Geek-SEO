# ContentFigures CLI

Legacy operator tool for **optional** DB-aware attach/sync of section AVIF files. **Canonical image generation** is the standalone [**SectionFigures**](../SectionFigures/README.md) CLI (HTTP read → OpenAI → disk; no database).

## Architecture

Section images are **layout slots outside post body**. geekatyourspot renders:

`public/images/{TechnicalArticle|Blog|Tool}/{department}/{pageSlug}/h2-{heading-slug}.avif`

via `next/image` in pillar/tool layouts — never inline in markdown. **No merge-into-body step** (operator veto — not planned work).

**Text** (headings, PAA `###` questions, prose) is stored in GeekAPI and rendered unfiltered by geekatyourspot.

## When to use this CLI vs SectionFigures

| Task | Tool |
|------|------|
| Generate images from briefs (OpenAI → AVIF on disk) | **SectionFigures** |
| Export briefs to `jobs.json` over HTTP | **SectionFigures** `export-jobs` |
| Optionally stamp `ImageUrl` / `Ready` in Postgres after manual attach | **ContentFigures** `attach` / `sync-dir` |

SectionFigures does **not** update the database. DB status in Content Writer UI is not a completion signal for site images — **disk files** are.

## Prerequisites

| Variable | Required for |
|----------|----------------|
| `CONTENT_WRITER_DATABASE_URL` | All ContentFigures commands |
| `CONTENT_IMAGE_OUTPUT_DIR` | `attach`, `sync-dir` |

Text must be published to GeekAPI first so each figure row has a `GeekApiSlug`.

## Commands

```bash
cd content-writer/backend
dotnet run --project tools/ContentFigures/ContentFigures.csproj -- list --project-id <guid>

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- attach \
  --project-id <guid> --source pillar --heading-slug <slug> --file ./art/h2-<slug>.avif

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- export-manifest \
  --project-id <guid> [--out manifest.json]

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- sync-dir \
  --project-id <guid> --source pillar --dir ./art/pillar
```

The `generate` subcommand is **deprecated** — use SectionFigures instead.

## Workflow

1. Generate figure briefs (Step 6) in Content Writer.
2. Publish text — pick department, publish to site.
3. **SectionFigures:** `export-jobs` → `plan` → `generate` → commit `public/images/...` → deploy geekatyourspot.
4. (Optional) ContentFigures `attach` if you want DB `ImageUrl` stamps — not required for the live site.
