# Feature spec: {FEATURE_NAME}

**Source URL:** {URL}  
**Captured:** {ISO_DATE}  
**Author:** Agent (deconstruct-web-feature skill)

---

## 1. Purpose

One paragraph: what user job this feature performs (e.g. "Shows daily keyword positions with trend sparklines and filters by location/device").

---

## 2. Entry points

| How user arrives | Route / nav |
|------------------|-------------|
| Sidebar item | `{label}` → `{path}` |
| Deep link | `{query params}` |

---

## 3. Layout map

```text
┌─────────────────────────────────────────┐
│ Page header (title, actions, date range)│
├──────────────┬──────────────────────────┤
│ Filters      │ Main content             │
│ (sidebar)    │ (table / chart)          │
└──────────────┴──────────────────────────┘
```

Attach screenshots: `screenshots/desktop-default.png`, `screenshots/mobile.png`.

---

## 4. Component inventory

| ID | Name | Type | Notes |
|----|------|------|-------|
| A | Page header | static | H1, breadcrumb, export button |
| B | Keyword table | interactive | sortable columns |
| C | Position sparkline | chart | 30-day mini chart per row |

---

## 5. Interaction model

**Primary driver:** `{click | scroll | hover | time | mixed}`

| Control | Trigger | Before state | After state | Transition |
|---------|---------|--------------|-------------|------------|
| Tab "Desktop" | click | shows mobile data | shows desktop | opacity 200ms |
| Scroll past 80px | scroll | nav expanded | nav compact | height 300ms ease |

---

## 6. Data flow

### 6.1 API calls observed

| Order | Method | Path pattern | Purpose | Request shape | Response shape |
|-------|--------|--------------|---------|---------------|----------------|
| 1 | GET | `/api/...` | load table | `{ projectId }` | `{ rows: [] }` |

### 6.2 GeekSEO mapping

| Observed | GeekSEO endpoint | Status |
|----------|------------------|--------|
| rankings list | `GET /api/seo/rankings/{projectId}` | exists |
| history sparkline | `GET /api/seo/rank-history/{projectId}` | **planned U2** |

### 6.3 Client state

| State | Storage | Fields |
|-------|---------|--------|
| filters | URL searchParams | `location`, `device` |
| table sort | React state | `sortBy`, `sortDir` |

---

## 7. Visual tokens (extracted)

| Token | Value | Usage |
|-------|-------|-------|
| Page background | `#...` | main |
| Primary text | `#...` | body |
| Accent / CTA | `#...` | buttons |
| Table row height | `...px` | |
| Font | `...` | |

**GeekSEO remap:**

| Token | GeekSEO value |
|-------|---------------|
| Primary | `#0e2d4e` |
| Accent | `#c4501a` |

---

## 8. shadcn mapping

| UI block | shadcn / project |
|----------|------------------|
| Table | `@/components/ui/table` + TanStack Table |
| KPI cards | `card` |
| Errors | `SeoErrorBanner` |

---

## 9. Implementation plan

| File | Action |
|------|--------|
| `frontend/src/app/app/rankings/page.tsx` | extend |
| `frontend/src/components/rankings/keyword-rank-table.tsx` | create |
| `frontend/src/lib/seo-api.ts` | add `fetchRankHistory()` when API exists |

---

## 10. Gaps vs source

| Item | Notes |
|------|-------|
| Export CSV | they have; we have partial |
| Backlink column | out of scope U5 |

---

## 11. Correctness checklist

- [ ] All data from GeekSeoBackend or GSC — no fake arrays in production path
- [ ] **STUBBED** sections listed explicitly
- [ ] `npm run build` passes
