# Competitor research output

Each subfolder is one **geek-scrape** run.

| Folder | Command | What it is |
|--------|---------|------------|
| `seranking/` | `site` | Multi-page crawl from https://seranking.com/ — open **`SITE-REPORT.md`** |
| `seranking-homepage/` | `page` | Homepage only — open **`SCRAPE-REPORT.md`** |
| `seranking/position-tracking/` | `page` | One feature URL |
| `_test/example/` | `page` + `--smoke` | Playwright smoke test — **not** real research |

## Commands

```bash
# Whole site (many pages under pages/*/)
npm run scrape:seranking

# One URL
npm run scrape -- page --url "https://seranking.com/position-tracking.html" \
  --out ./docs/research/competitors/seranking/position-tracking --network --full-page
```
