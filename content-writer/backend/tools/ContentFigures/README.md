# ContentFigures CLI

Operator tool for saving section AVIF files and tracking Content-Writer `content_figures` rows. **Not part of the geekatyourspot Next.js app.**

## Architecture

Section images are **layout slots outside post body**. geekatyourspot renders:

`public/images/{TechnicalArticle|Blog|Tool}/{department}/{pageSlug}/h2-{heading-slug}.avif`

via `next/image` in pillar/tool layouts — never inline in markdown. There is no merge-into-body step.

## Prerequisites

| Variable | Required for |
|----------|----------------|
| `CONTENT_WRITER_DATABASE_URL` | All commands |
| `CONTENT_IMAGE_OUTPUT_DIR` | `attach`, `sync-dir`, `generate` (site-static default) |
| `BLOB_READ_WRITE_TOKEN` | `attach`/`generate` when `CONTENT_IMAGE_STORAGE=vercel_blob` |

Text must be published to GeekAPI first so each figure row has a `GeekApiSlug`.

## Commands

```bash
cd content-writer/backend
dotnet run --project tools/ContentFigures/ContentFigures.csproj -- list --project-id <guid>

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- attach \
  --project-id <guid> --source pillar --heading-slug <slug> --file ./art/h2-<slug>.avif

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- set-url \
  --project-id <guid> --source pillar --heading-slug <slug> --url https://...

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- skip \
  --project-id <guid> --source pillar --heading-slug <slug>

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- export-manifest \
  --project-id <guid> [--out manifest.json]

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- sync-dir \
  --project-id <guid> --source pillar --dir ./art/pillar

# Purge stored images (columns stay in DB)
dotnet run --project tools/ContentFigures/ContentFigures.csproj -- purge-all
```

## Workflow

1. Generate figure briefs (Step 6) in Content-Writer.
2. Publish text — pick department, publish to site.
3. For each section: save AVIF to the target path (`attach`, `sync-dir`, upload in UI, or generate & save).
