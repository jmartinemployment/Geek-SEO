# ContentImageSpike

Legacy console bake-off tool. Prefer **https://seo.geekatyourspot.com/image-generator** for day-to-day drafts.

OpenAI (`gpt-image-1`) only — Leonardo was removed.

## Run

From `backend/`:

```bash
dotnet run --project tools/ContentImageSpike -- \
  --project-id <your-project-guid> \
  --output-dir output/image-spike
```

Requires `OPENAI_API_KEY` and Content Writer DB connection.
