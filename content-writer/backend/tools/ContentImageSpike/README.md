# ContentImageSpike

Standalone console tool to **bake off image providers** (OpenAI DALL·E vs Leonardo) using real Content Writer project data from the database. Not wired into the main Content Writer API — follows SOLID with separate reader, prompt builders, providers, and file writer.

## What it generates

| Use case | Source in DB | Prompt intent | Size |
|----------|--------------|---------------|------|
| `pillar-figure` | `TechnicalArticle` | Flat infographic / teaching diagram | 1536×1024 |
| `social-facebook` | `SocialFacebook` | Eye-candy card background | 1200×630 |
| `social-linkedin` | `SocialLinkedIn` | Eye-candy card background | 1200×630 |

Skips use cases when the project has no matching `GeneratedContent` row.

## Run

From `backend/`:

```bash
dotnet run --project tools/ContentImageSpike -- \
  --project-id <your-project-guid> \
  --provider both \
  --output-dir output/image-spike
```

### Options

- `--project-id` (required) — Content Writer project GUID
- `--provider` — `openai`, `leonardo`, or `both` (default: both)
- `--output-dir` — where images are saved (default: `output/image-spike`)

### Environment

| Variable | Purpose |
|----------|---------|
| `CONTENT_WRITER_DATABASE_URL` or `DATABASE_URL` | Postgres (production / Supabase) |
| `ConnectionStrings:ContentWriterDb` in API `appsettings` | SQLite path for local dev |
| `OPENAI_API_KEY` | DALL·E 3 |
| `LEONARDO_API_KEY` | Leonardo REST API |

Optional `appsettings.json` in this folder:

```json
{
  "ImageSpike": {
    "OutputDirectory": "output/image-spike",
    "LeonardoModelId": "de7d3faf-762f-48e0-b3b7-9d0ac3a3fcf3"
  }
}
```

## Output layout

```
output/image-spike/<projectIdN>/
  openai_pillar-figure_20260708_143022.png
  openai_pillar-figure_20260708_143022.meta.txt
  leonardo_social-linkedin_20260708_143045.png
  ...
```

Compare providers side-by-side before integrating into Content Writer.

## Architecture

```
IContentImageSourceReader     → ContentWriterImageSourceReader (EF Core)
IImagePromptBuilder           → PillarFigurePromptBuilder, SocialEyeCandyPromptBuilder
IImageGenerationProvider      → OpenAiImageProvider, LeonardoImageProvider
IImageArtifactWriter          → LocalImageArtifactWriter
ImageSpikeService             → orchestrates read → prompt → generate → save
```
