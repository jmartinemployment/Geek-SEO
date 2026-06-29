---
name: deconstruct-web-feature
description: >-
  Reverse-engineer a single page or UI feature from a URL using browser MCP:
  read DOM, computed styles, network calls, and interaction logic; write a
  feature spec; generate GeekSEO-ready React/Next.js components with shadcn/ui.
  Use when the user wants to clone a dashboard panel, replicate a competitor
  screen (SE Ranking, Ahrefs, etc.), deconstruct frontend logic, or output
  modular Shadcn components from a live site. Requires Playwright MCP or
  equivalent browser automation. Not for full-site clones — use clone-website
  for that.
argument-hint: "<url> [feature-name e.g. rank-tracker]"
user-invocable: true
---

# Deconstruct Web Feature (MCP + Shadcn)

You analyze **one URL** (or one authenticated screen after login) and produce:

1. A **feature spec** (behavior, data flow, UI states) — saved under `docs/research/features/`
2. **GeekSEO implementation plan** — which routes, APIs, and shadcn components to use
3. Optional: **React/SCSS files** in `frontend/src/` aligned with Geek @ Your Spot brand

This skill is for **competitive UX research**, **cloning or copying competitor features/components** into GeekSEO (your layout, APIs, and brand), and **dashboard upgrades** (see [`plan-documents/TODO.md`](../../../plan-documents/TODO.md) + [`plan-documents/SEO-PROVIDER-STRATEGY.md`](../../../plan-documents/SEO-PROVIDER-STRATEGY.md) for provider boundaries). For full marketing-site pixel clones, use `clone-website` instead.

---

## Prerequisites

### Browser MCP (required)

Use whichever is available in Cursor:

| Tool | Use for |
|------|---------|
| **Playwright MCP** | Navigate, screenshot, evaluate JS in page, network tab |
| **Chrome DevTools MCP** | DOM + computed styles |
| **cursor-ide-browser** | Same, if configured |

If **no** browser MCP is available: run `npm run scrape -- page --url … --out docs/research/competitors/<name>/<feature>` for one screen, or `site` to crawl the whole public site (see [`scripts/scrape/README.md`](../../../scripts/scrape/README.md)).

### Project context

- **Frontend:** `frontend/` — Next.js App Router, React 19, Tailwind 4, shadcn patterns under `frontend/src/components/`
- **Brand tokens (GeekSEO):** navy `#0e2d4e` / `#0A0B26`, accent `#c4501a`, data-dense dashboard UI
- **Backend:** do not stub data — wire to existing `frontend/src/lib/seo-api.ts` endpoints or flag **STUBBED** with required API

### shadcn MCP (optional, for implementation)

```bash
cd frontend && npx shadcn@latest mcp init   # once per machine
```

Use `shadcn:search_items_in_registries` / `shadcn:view_items_in_registries` to pick `table`, `card`, `chart`, `tabs`, etc. See [`.agents/skills/shadcn/mcp.md`](../shadcn/mcp.md).

---

## Workflow (follow in order)

### Step 1 — Scope the feature

From `$ARGUMENTS` (URL + optional feature name):

- Confirm **URL** loads (or document login wall).
- Name the feature (e.g. `seranking-rank-tracker`, `ahrefs-site-audit-overview`).
- Define **in scope:** layout, interactions, empty/loading/error states, responsive breakpoints.
- Define **out of scope:** backend replication, auth bypass, paywalled data — use **mock JSON shaped like GeekSEO APIs** only if APIs do not exist yet (mark **STUBBED**).

Create folder:

```text
docs/research/features/<feature-slug>/
  spec.md
  screenshots/
  network/
  assets/
```

### Step 2 — Reconnaissance via browser MCP

**Static layer**

1. Full-page screenshot (desktop 1440×900).
2. Screenshot mobile (375×812) if responsive matters.
3. In-page JS evaluation — capture:
   - Framework hints: `__NEXT_DATA__`, `window.__NUXT__`, React root, Vue app
   - Global CSS variables: `getComputedStyle(document.documentElement)` → custom properties
   - Font families loaded (`document.fonts`)

**DOM / layout layer**

For the target feature root selector (e.g. `[data-testid="rank-tracker"]` or main content region):

```javascript
(() => {
  const root = document.querySelector('MAIN_SELECTOR');
  if (!root) return { error: 'selector not found' };
  const cs = getComputedStyle(root);
  return {
    tag: root.tagName,
    className: root.className,
    childCount: root.children.length,
    rect: root.getBoundingClientRect().toJSON(),
    display: cs.display,
    gridTemplateColumns: cs.gridTemplateColumns,
    gap: cs.gap,
  };
})();
```

Document **component tree** (max 3 levels deep in spec): regions → widgets → controls.

**Behavior layer** (critical)

Answer before writing code:

| Question | How to verify |
|----------|----------------|
| Click vs scroll driven? | Scroll slowly first; then click tabs |
| What triggers loading? | Network panel + UI spinners |
| What API calls fire? | Filter XHR/fetch; record method, path pattern, payload shape |
| Stateful UI? | Click every tab/filter; screenshot each state |
| URL routing? | Change filters; observe `history.pushState` |

Record **INTERACTION MODEL** explicitly in `spec.md`.

**Network layer**

Export or transcribe:

- API base URL(s)
- Example request/response JSON (redact secrets)
- Polling vs websocket vs one-shot fetch

Map each call to **GeekSEO equivalent** if any:

| Their call | GeekSEO endpoint |
|------------|------------------|
| `/api/rankings/...` | `GET /api/seo/rankings/{projectId}` |
| unknown | **Needs new endpoint** (cite U* from upgrade plan) |

### Step 3 — Write `spec.md`

Use template: [`references/spec-template.md`](references/spec-template.md).

The spec is the **contract**. Do not implement until spec sections 1–7 are filled.

### Step 4 — Map to shadcn + GeekSEO

| UI pattern | shadcn / project component |
|------------|----------------------------|
| Data table with sort | `table` + `@tanstack/react-table` pattern in repo |
| KPI row | `card` grid — see `frontend/src/app/app/dashboard` |
| Charts | `recharts` — see existing audit/rankings pages |
| Filters | `select`, `dropdown-menu`, `tabs` |
| Errors | `SeoErrorBanner` — `frontend/src/components/seo/seo-error-banner.tsx` |

Install missing shadcn components via MCP or:

```bash
cd frontend && npx shadcn@latest add table card tabs select
```

**Rebrand:** replace their colors with CSS variables:

```css
--geek-navy: #0e2d4e;
--geek-accent: #c4501a;
```

### Step 5 — Generate code

**Rules:**

- Standalone React components under `frontend/src/components/<area>/`
- Use existing `seo-api.ts` functions; add new functions only with matching backend ticket
- `ChangeDetectionStrategy.OnPush` + signals if new Angular — **this project is React**: use hooks + `@tanstack/react-query` if already in page
- No `any`; types in `frontend/src/lib/` or colocated `*.types.ts`
- Verify: `cd frontend && npm run build`

**Deliverables checklist:**

- [ ] `spec.md` committed
- [ ] Component(s) + page route or section integration
- [ ] Loading / empty / error states
- [ ] Data source documented (real API vs **STUBBED**)

### Step 6 — Verification

1. `cd frontend && npm run build`
2. `cd frontend && npm run test` (if tests added)
3. Compare screenshot side-by-side with research screenshots (describe gaps in spec § Gaps)

---

## MCP bridge pattern (how it works)

```text
  User: "Deconstruct rank tracker from https://seranking.com/..."
       │
       ▼
  Cursor Agent + this skill
       │
       ├── Playwright MCP ──► live DOM, styles, clicks, network
       │
       ├── Write spec.md (human-readable contract)
       │
       ├── shadcn MCP ──► pick/install components
       │
       └── Edit frontend/src ──► GeekSEO-branded implementation
```

SerpApi MCP and DataForSEO are **data** providers — not used for layout cloning. Use browser MCP for UI only.

---

## Fallback: geek-scrape (no MCP)

```bash
npm run scrape -- page \
  --url "https://competitor.com/app/rankings" \
  --selector "main" \
  --out docs/research/competitors/example/rank-tracker \
  --network
```

Outputs `page.json`, `content.md`, `links.json`, optional `network.json`, and a screenshot. Feed these into the agent to complete `spec.md` and code.

---

## Anti-patterns

- Guessing hex colors or spacing — extract `getComputedStyle` or mark unknown
- Building click-tabs when original is scroll-synced (see clone-website § interaction model)
- Shipping fetch to `localStorage` instead of GeekSeoBackend
- Copying competitor trademarks/logos into production UI
- Full page clone when user asked for one feature — scope down

---

## Related skills

| Skill | When |
|-------|------|
| [`clone-website`](../../../../../../.claude/skills/clone-website/SKILL.md) | Full marketing site pixel clone |
| [`shadcn`](../shadcn/SKILL.md) | Component install and registry |
| [`web-design-guidelines`](../web-design-guidelines/SKILL.md) | Accessibility/UX audit after build |
| [`playwright-best-practices`](../../../../../../.claude/skills/playwright-best-practices/SKILL.md) | E2E tests for new screen |

---

## Example invocation

> Deconstruct the SE Ranking rank tracker table from https://seranking.com — output spec and a GeekSEO `/app/rankings` upgrade using our brand colors and real GSC/rank-history APIs.

Agent should: run Steps 1–6, save to `docs/research/features/seranking-rank-tracker/`, implement only after spec is complete.
