# SectionFigures CLI

Standalone operator tool for section art: **HTTP read** from Content Writer, **OpenAI** image generation, **AVIF on disk** only. No Postgres, no EF, no `ContentWriter.Application` reference, no DB write-back.

geekatyourspot renders images from layout slots checking `public/images/{TechnicalArticle|Blog|Tool}/...` — not from post body HTML or GeekAPI image URL fields.

## Prerequisites

| Variable | Required for |
|----------|----------------|
| `CONTENT_WRITER_API_URL` | `export-jobs`, `plan`/`generate` with `--project-id` |
| `CONTENT_WRITER_API_KEY` | Optional Bearer token if API requires auth |
| `CONTENT_IMAGE_OUTPUT_DIR` | `plan`, `generate` — path to `geekatyourspot/public` |
| `OPENAI_API_KEY` | `generate` |

Optional overrides: `SECTION_FIGURES_OPENAI_MODEL` (default `dall-e-3`), `SECTION_FIGURES_OPENAI_SIZE` (default `1792x1024`).

## Commands

```bash
cd content-writer/backend

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- export-jobs \
  --project-id <guid> --out jobs.json

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- plan --jobs jobs.json

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- generate \
  --jobs jobs.json --yes --concurrency 4
```

### Idempotency

`generate` skips jobs whose target AVIF already exists under `CONTENT_IMAGE_OUTPUT_DIR`. Use `--force` to overwrite.

### Partial failures

On OpenAI errors, the batch **continues** (unless `--fail-fast`), leaves no partial file for failed jobs, exits **non-zero** with a summary count.

### Cost guard

`plan` prints image count × estimated per-image price. `generate` requires `--yes` when more than 5 images would be generated.

## Operator workflow

1. Publish text in Content Writer (figures API must return `geekApiSlug`).
2. `export-jobs` → `plan` → review cost and paths.
3. `generate --yes` → spot-check 3–5 AVIFs locally.
4. Commit `public/images/{TechnicalArticle,Blog,Tool}/**` in geekatyourspot; deploy Vercel.
5. Verify live site layout slots show images (filesystem is source of truth — not Content Writer DB status).

Multi-project runs: accumulate all AVIFs, then **one git commit** at the end.

## Vetoed (do not build)

- Auto figure insertion into GeekAPI post bodies
- Figure merge on publish
- In-app “Generate & save” in Content Writer as the canonical pipeline
- DB `Ready`/`Pending` as image completion signal for the live site

See also [`../ContentFigures/README.md`](../ContentFigures/README.md) for optional legacy DB attach.
