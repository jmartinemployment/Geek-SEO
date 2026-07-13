# SectionFigures

**One section at a time:** Content Writer briefs → OpenAI draft → Figma polish → AVIF on disk. Agent-driven in Cursor; no batch, no web UI, no in-app Content Writer generation.

## Workflow

1. Agent: `export-jobs` → `plan` (checklist of paths + disk status).
2. Agent: `generate-one --heading-slug <slug>` → you review.
3. You: refine in Figma if needed; export AVIF to path from `plan`.
4. Repeat per section; commit `public/images/...` in geekatyourspot.

## Env vars

| Variable | Required |
|----------|----------|
| `CONTENT_WRITER_API_URL` | export / plan |
| `CONTENT_IMAGE_OUTPUT_DIR` | plan, generate-one (`geekatyourspot/public`) |
| `OPENAI_API_KEY` | generate-one |
| `CONTENT_WRITER_API_KEY` | optional |

## Commands

```bash
cd content-writer/backend

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- export-jobs \
  --project-id <guid> --out jobs.json

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- plan --jobs jobs.json

dotnet run --project tools/SectionFigures/SectionFigures.csproj -- generate-one \
  --jobs jobs.json --heading-slug overview
```

Compose prompt: flat vector infographic flowcharts.

## Vetoed

- Batch generate
- Batch web UI
- In-app Generate & save in Content Writer
- Figure merge into post bodies
