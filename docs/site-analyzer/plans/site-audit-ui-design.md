# Site Audit UI design (Semrush-inspired)

**Reference:** [Semrush Site Audit Overview](https://www.semrush.com/kb/540-site-audit-overview), [Issues report](https://www.semrush.com/kb/541-site-audit-issues-report) (authenticated dashboard; URL hash `sorting/update_asc/page/1` = issues table sort + pagination).

**Goal:** Project-scoped health dashboard — **not** the keyword pillar flow on `page.tsx` today.

---

## Information architecture

**Site Audit is not part of Create Site Profile.** Same Project URL, different operator action:

| Control | Track | What it does |
|---------|-------|----------------|
| **Create Site Profile** | Project | Homepage fetch → business identity, themes, writing recs |
| **Run Site Audit** | Project | Full-site crawl → health score, errors/warnings, issues table |

```text
Project URL (site_profiles)
├── Project setup
│   ├── Create Site Profile     ← Step 1 (POST /sites)
│   └── Run Site Audit          ← Step 2 (POST /sites/{id}/audit) — separate method
├── Keyword Overview            ← Steps 3+ (per keyword; no site profile in report)
├── Keyword Gap                 ← Phase 2b
└── Competitor overview         ← Phase 2c (Semrush Domain Overview; ≤5 domains, one report)
```

Content Writer handoff stays on **Keyword research** only until CW needs audit fields.

---

## Semrush → Site Analyzer mapping

| Semrush (Overview) | Our panel | Data source |
|--------------------|-----------|-------------|
| Site Health % | `healthScore` /100 | `SiteAuditRollupService` |
| Pages crawled breakdown | healthy / issues / broken | `site_audit_runs` + page HTTP status |
| Errors / Warnings / Notices counts | same three chips | `findings` by `audit_severity` |
| Top issues | short list (top 5) | sort by `affectedPageCount × weight` |
| Thematic reports | category cards with % | `category_rollups_json` |
| Rerun campaign | **Run audit** / **Rerun** | `POST /sites/{id}/audit` |
| Issues tab table | full issues list | `GET …/issues` with sort + page |
| Issue detail → URL list | drill-down route (2a+) | `payload_json.urls` |
| Why and how to fix | `fixGuide` on each issue | static copy per `audit_issue_code` |

---

## Layout wireframe (Site Audit tab)

Matches existing operator UI: dark zinc, `dl`/`dt` panels, compact tables.

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ geekatyourspot.com                                    [Run audit] [Rerun] │
│ Last audit: Jun 26, 2026 · 142 pages crawled                              │
├──────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────┐  Healthy 118   With issues 20   Broken 4   Blocked 0     │
│ │  Site health │                                                          │
│ │    87/100    │  Errors 12 · Warnings 31 · Notices 8                     │
│ └─────────────┘                                                          │
├──────────────────────────────────────────────────────────────────────────┤
│ Thematic reports                                                         │
│ ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐              │
│ │Crawlability│ │   HTTPS    │ │  Markups   │ │ Internal   │  (later)     │
│ │    94%     │ │   100%     │ │    72%     │ │    —       │              │
│ │ 2 errors   │ │ 0 errors   │ │ 8 warnings │ │            │              │
│ └────────────┘ └────────────┘ └────────────┘ └────────────┘              │
├──────────────────────────────────────────────────────────────────────────┤
│ Top issues                                    [View all issues →]          │
│ • Missing title tag · Markups · 18 pages                                 │
│ • No JSON-LD · Markups · 12 pages                                        │
│ • Page returns 404 · Crawlability · 4 pages                              │
├──────────────────────────────────────────────────────────────────────────┤
│ Issues   [All] [Errors] [Warnings] [Notices]                             │
│ Filter: Category ▼   Search issue…                                       │
│ ┌────────────────────────────────────────────────────────────────────┐   │
│ │ Issue ▲          │ Category    │ Pages │ Updated ▼ │ Fix guide   │   │
│ │ Missing title    │ Markups     │ 18    │ Jun 26      │ Why / how   │   │
│ │ No JSON-LD       │ Markups     │ 12    │ Jun 26      │ Why / how   │   │
│ │ HTTP not HTTPS   │ HTTPS       │ 4     │ Jun 26      │ Why / how   │   │
│ └────────────────────────────────────────────────────────────────────┘   │
│ Page 1 of 3   ‹ ›                                                        │
└──────────────────────────────────────────────────────────────────────────┘
```

**Empty state:** no audit yet → large “Run your first site audit” + short explanation (Semrush free checker CTA analog).

**Running state:** health card shows spinner; reuse SSE pattern when progress logging lands (not REST polling).

---

## Issues table (Semrush `#sorting/…/page/…`)

| Column | Sortable | Notes |
|--------|----------|-------|
| Issue | yes | `audit_issue_code` display name |
| Category | yes | `SiteAuditCategory` filter |
| Pages | yes (default desc) | affected URL count |
| Updated | yes | last audit `crawl_finished_at` |
| Fix guide | no | expand row or modal |

API query params (planned):

```text
GET …/issues?severity=error|warning|notice
            &category=Crawlability|Https|Markups
            &sort=pages_desc|pages_asc|issue_asc|updated_desc
            &page=1&pageSize=25
```

---

## Component split (Web, slice 2a-5)

| Component | Responsibility |
|-----------|----------------|
| `SiteAuditTab` | tab shell, load latest overview |
| `SiteAuditHealthCard` | score + page breakdown |
| `SiteAuditSeverityChips` | errors / warnings / notices |
| `SiteAuditThematicGrid` | category cards |
| `SiteAuditTopIssues` | top 5 list |
| `SiteAuditIssuesTable` | sortable paginated table |
| `SiteAuditFixGuide` | static copy per issue code |

Keep in `page.tsx` until tab split is warranted (same as current keyword panels).

---

## v1 categories (crawl + extract only)

| Category | First checks |
|----------|----------------|
| Crawlability | 4xx/5xx, orphan pages, depth > N |
| HTTPS | `http://` URLs on crawled pages |
| Markups | missing title, meta description, H1, JSON-LD |

Deferred: Core Web Vitals, International SEO, AI Search Health (need external signals or heuristics ADR).

---

## Approaches considered

| Approach | Pros | Cons |
|----------|------|------|
| **A. Separate tab + `site_audit_runs`** (chosen) | Clear Semrush parity; no keyword run pollution | New schema + job |
| B. Audit section on keyword page | Faster to ship | Wrong unit of work; confusing with pillar runs |
| C. Embed in Geek-SEO | Single app for operators | Violates sa2 owns facts; CW stays consumer |

**Recommendation:** A — matches ADR 013 and Phase 2 plan.
