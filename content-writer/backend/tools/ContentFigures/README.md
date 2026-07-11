# ContentFigures CLI (Phase 2)

Operator tool for attaching section art to Content-Writer `content_figures` rows. Connects directly to Postgres (`CONTENT_WRITER_DATABASE_URL`) — not through the HTTP API.

## Prerequisites

| Variable | Required for |
|----------|----------------|
| `CONTENT_WRITER_DATABASE_URL` | All commands |
| `BLOB_READ_WRITE_TOKEN` | `attach`, `sync-dir` |
| `GEEK_BACKEND_API_KEY` | `merge` |

Text must be published to GeekAPI first so each figure row has a `GeekApiSlug`.

## Commands

```bash
cd content-writer/backend
dotnet run --project tools/ContentFigures/ContentFigures.csproj -- list --project-id <guid>

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- attach \
  --project-id <guid> --source pillar --heading-slug <slug> --file ./art/h2-<slug>.webp

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- skip \
  --project-id <guid> --source pillar --heading-slug <slug>

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- export-manifest \
  --project-id <guid> [--out manifest.json]

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- sync-dir \
  --project-id <guid> --source pillar --dir ./art/pillar

dotnet run --project tools/ContentFigures/ContentFigures.csproj -- merge \
  --project-id <guid> --source pillar
```

## Rules (binary)

- `attach` requires `.webp`, a matching figure row, and `GeekApiSlug` on that row.
- `attach` fails on `Skipped` figures.
- `sync-dir` fails if any `h2-*.webp` file cannot be matched to a heading slug.
- Blob path: `content/{geekApiSlug}/{sourceType}/h2-{headingSlug}.webp`

## Workflow

1. Generate figure briefs (Step 6) in Content-Writer.
2. Publish text — pick department, publish to site.
3. Create art in Figma/Cursor; export WebP named `h2-{headingSlug}.webp`.
4. `attach` or `sync-dir` — figures become `Ready` with `NeedsFigureMerge = true`.
5. `merge` (or republish from Content Writer) — inserts figures into GeekAPI body and sets `Published`.
