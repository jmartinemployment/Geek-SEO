# SectionFigures

Helper for **agent-driven** section art: Content Writer briefs → OpenAI draft → Figma polish → AVIF on disk. No Postgres, no in-app Content Writer generation, no DB write-back.

geekatyourspot renders layout slots from `public/images/{TechnicalArticle|Blog|Tool}/...` only.

## Intended workflow

1. **Cursor agent:** `export-jobs` → `plan` (checklist of paths + what exists on disk).
2. **One section at a time:** `generate-one --heading-slug <slug>` → review PNG/AVIF.
3. **You in Figma:** refine acceptable drafts; export AVIF to the path from `plan`.
4. **Agent:** re-run `plan` until gaps are closed; commit `public/images/...` in geekatyourspot.

Batch `generate` exists but is optional — not the default operator path.

## Env vars

| Variable | Required for |
|----------|----------------|
| `CONTENT_WRITER_API_URL` | `export-jobs`, `plan` / `generate*` with `--project-id` |
| `CONTENT_WRITER_API_KEY` | Optional Bearer token |
| `CONTENT_IMAGE_OUTPUT_DIR` | `plan`, `generate*` — path to `geekatyourspot/public` |
| `OPENAI_API_KEY` | `generate-one`, `generate` |

## Commands

```bash
cd content-writer/backend

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- export-jobs \
  --project-id <guid> --out jobs.json

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- plan --jobs jobs.json

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- generate-one \
  --jobs jobs.json --heading-slug overview

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- generate \
  --jobs jobs.json --yes   # batch only if you want it
```

Compose prompt: flat vector infographic flowcharts (same style that worked when agents generated images manually).

## Vetoed

- Batch web UI
- In-app “Generate & save” in Content Writer
- Auto figure insertion / merge into post bodies
- DB `Ready`/`Pending` as live-site image truth

See [`../ContentFigures/README.md`](../ContentFigures/README.md) for optional legacy DB attach.
