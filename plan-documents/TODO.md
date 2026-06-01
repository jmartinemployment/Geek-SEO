# Geek SEO — TODO (all remaining work)

**Single planning queue** after retiring `geekseo-plan.md` (June 2026).

| Read this | When you need |
|-----------|----------------|
| **[`TODO.md`](TODO.md)** (this file) | What to build next |
| **[`PROJECT_STATUS.md`](../PROJECT_STATUS.md)** | What is live in production; parity #1–27 status |
| **[`ARCHITECTURE.md`](ARCHITECTURE.md)** | Services, ports, data flow, API surface |
| **[`docs/ROADMAP.md`](../docs/ROADMAP.md)** | One-screen index |
| **[`GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md`](GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md)** | **Geek Data Plane** — provider strategy, interfaces, SerpApi bridge, DataForSEO → zero |

**v1 checklist closure (June 2026):** Parity **#1–27** shipped in repo (waivers below). Not a statement that the product beats Surfer/Frase in the editor.

When an item ships, update [`PROJECT_STATUS.md`](../PROJECT_STATUS.md) and check it off here.

---

## Product context (unchanged goal)

Clone core workflows of **Surfer SEO**, **ContentShake**, and **Frase** at **$29–$149/mo** for SMBs and freelance SEOs.

**Primary loop:** keyword research → brief → AI draft → live score → publish → decay monitor → AI visibility.

**Known weakness:** Editor scoring is v1 math (`ContentScoringService.cs`) — six labeled components but thin SERP term coverage vs competitors. See **Scoring & editor** below.

**Scoring (v1 as-built):** `GeekSeo.Application/Services/ContentScoringService.cs`, `GeoScoringCalculator.cs` · UI: `frontend/src/components/editor/score-sidebar.tsx`

**Architecture:** Browser → GeekSeoBackend (:5051) → GeekAPI internal → GeekRepository → `geek_seo`. Never GeekAPI as SEO product host. See [`ARCHITECTURE.md`](ARCHITECTURE.md).

---

## Scoring & editor (product gap — not v1 complete)

Competitors win on **SERP term checklist + score that moves when you add specific terms**. Current v1 uses keyword word-split for “term coverage.”

| Priority | Work |
|----------|------|
| P0 | Build **SERP term set** from crawled competitors (cache per keyword/location; crawl plumbing exists) |
| P0 | Score **term coverage** against that set; suggestions name missing terms + point value |
| P1 | Editor **term table** (used / missing / recommended count), not only six progress bars |
| P1 | **Auto-optimize** wired to top missing terms (not generic “use phrase more”) |
| P2 | Optional: separate **SCORING-V2.md** spec once scope is agreed — do not revive deleted `geekseo-content-scoring-spec.md` |

---

## #12b topical map — polish

| Step | Work |
|------|------|
| V2.2 | Market **opportunities** — keyword-discovery provider diff vs GSC-only gaps (`coverage: opportunity` enrichment) — see [`GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md`](GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md) |
| V2.4 | Dashboard **“Do this next”** panel wired to `GET /api/seo/topical-map` `recommendations` |
| V2.5 | Playwright **E2E** — GSC test project → generate map → open gap → create document *(subset of E2E below)* |

**Already shipped (Jun 2026):** V2.0–V2.1, V2.3, map-page recommendations rail, competitor domains on clusters, planner `TopicClusteringService` clustering.

---

## Billing, E2E, tests (ex–Steps 29–31)

| Step | Work | Reference |
|------|------|-----------|
| **29** | **Billing sign-off** — `/pricing` tier cards + PayPal subscription + webhook tier sync + cancel flow; all gated routes respect live `seo_subscriptions` row. Sandbox shipped; **live charges** → P4 below. | [`docs/PAYPAL-BILLING.md`](../docs/PAYPAL-BILLING.md) |
| **30** | **Playwright E2E — all clone flows** — OAuth login; guided wizard → publish; editor SignalR score; planner → editor; topical map (GSC test project); content guard approve; calendar drag-drop; auto-optimize undo; detect + plagiarism gate. CI per `scripts/E2E_SMOKE.md`. | `e2e/README.md` |
| **31** | **Unit tests — scoring and gates** — all 6 SEO components, 5 GEO dimensions, term benchmarks (when term matrix exists), feature gate matrix, usage cap enforcement (expand beyond current 13 Vitest + partial xUnit). | `GeekSeoBackend.Tests` |

---

## Parity gaps (waived from v1 closure)

| # | Item | Notes |
|---|------|--------|
| **6** | GPTZero-branded AI detection UI | Detection/humanize exists in editor; branding not Surfer-parity |
| **15** | WordPress REST publish — production E2E | Code shipped; operator has no WP site for live QA |
| **19** | Content Guard — WP draft push E2E | Decay scan + patch + approve in DB work; WP draft path unverified |
| **20** | Multi-LLM GEO (ChatGPT, Gemini, Perplexity) | Google AIO/organic probe + trends shipped; other LLMs not built |

---

## Integrations (parity #28–31 — not built)

Build order when picked up: **#31 public API** → **#28 WP plugin** → **#29 Chrome** → **#30 Google Docs**.

| # | Feature |
|---|---------|
| 28 | WordPress plugin (score column, editor sidebar, deep link) |
| 29 | Chrome extension MV3 (SERP popup, WP editor overlay) |
| 30 | Google Docs add-on (sidebar score + top 5 terms) |
| 31 | OpenAPI + `/api/seo/v1/*` + Agency API keys (120 req/min) |

---

## REDESIGN — deferred phases

| Phase | Goal |
|-------|------|
| **2** | Public home scan (URL + keyword), PSI + public SERP API |
| **2b-min** | `/tools` hub (word counter, SERP simulator, etc.) |
| **2b-full** | DataForSEO + AI public tools at `/tools` |
| **3** | Dashboard **Copilot → Claude API** (replace rule-based suggestions from content scores) |
| **7** | Editor research rail (Frase-style) + Copilot from map/audit data |

| Phase | Optional future (not v2) |
|-------|---------------------------|
| 4-alt | Sitemap crawl → NLP → Claude pillar tree (original REDESIGN spec) |
| 5-alt | Site focus input + dashboard “Topical coverage %” column |
| 8-alt | Free `/tools/ai-visibility` marketing tool |

---

## P4 — Ops & go-live (ex–Steps 32–34)

| Item | Done when | Doc |
|------|-----------|-----|
| PayPal **live** (real charges) — completes Step 29 | Webhook activates tier; gated routes enforce subscription | [`docs/PAYPAL-BILLING.md`](../docs/PAYPAL-BILLING.md) |
| Production identity | `@geekatyourspot.com` worker + project `UserId` migration | [`PROJECT_STATUS.md`](../PROJECT_STATUS.md) § Identity |
| **Step 32 — Production deploy sign-off** | `geek_seo` migrated; GeekSeoBackend `/health` green; Vercel `seo.geekatyourspot.com`; SignalR OK; cold keyword &lt; 40s, warm &lt; 3s | Railway + Vercel env |
| **Step 33 — Dogfood + launch proof** | geekatyourspot.com GSC; 5 local-keyword articles via WP REST; 2–4 weeks audit data; landing uses real screenshots | Blocked until WP staging |
| **Step 34 — SignalR scale-out** | Second GeekSeoBackend instance; Redis backplane; score reaches clients on either instance | `AddStackExchangeRedis()` + Railway Redis |
| WordPress dogfood for #15 / #19 QA | Staging WP exists | — |

---

## Post-v1 upgrade track

**Data plane (canonical):** [`GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md`](GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md) — Phases A–D (SerpApi bridge → keywords off DFS → `GeekSerpProvider` → remove `DATAFORSEO_*`). Rank tracker migration: replace `DataForSeoRankSnapshotProvider` per that plan.

**Agency / SE Ranking–class UI** (white-label, reports): backlog here and in integrations — no separate upgrade file; provider work stays in Geek Data Plane.

---

## Security & billing (pre–public paid launch)

| Priority | Item | Reference |
|----------|------|-----------|
| P0 | Reject prod `X-User-Id` impersonation | [`docs/CODE-REVIEW.md`](../docs/CODE-REVIEW.md) |
| P0 | Server-bind PayPal subscription to user | CODE-REVIEW §2 |
| P0 | Fail-closed tier/usage gates | CODE-REVIEW §3 |
| P0 | Fix double usage increment | CODE-REVIEW |
| P1 | Redis/DB Google OAuth state (multi-instance) | CODE-REVIEW |
| P1 | `SUBSCRIPTION_MANUAL_TIER_ENABLED=false` on prod | CODE-REVIEW |

---

## Audit / polish (optional)

| Item | Notes |
|------|--------|
| Site audit background worker + PageSpeed wiring | REDESIGN Phase 6 remainder |
| GEO Phase 8 marketing surface | Beyond on-demand `/app/geo` |
| Lighthouse-specific health score vs crawl score | Dashboard “Site Health” column |

---

## Reference — parity features #1–31 (scope list)

Shipped status per feature: [`PROJECT_STATUS.md`](../PROJECT_STATUS.md). This table is the scope index only.

| # | Feature |
|---|---------|
| 1 | Real-time editor + live SEO score (SignalR) |
| 2 | Content brief generator |
| 3 | One-click full article |
| 4 | Bulk article generation |
| 5 | AI humanizer |
| 6 | AI content detection |
| 7 | Auto-optimize |
| 8 | Auto internal linking |
| 9 | Brand voice profiles |
| 10–11 | Content Planner / Topic Research |
| 12 / 12b | Topical map (GSC) / strategy map (SERP clusters) |
| 13 | Deep SERP analyzer |
| 14 | Keyword cannibalization |
| 15 | WordPress REST publish |
| 16 | Content calendar |
| 17 | Guided SMB wizard |
| 18 | Published content audit |
| 19 | Content Guard |
| 20 | Multi-LLM AI visibility |
| 21–23 | Dual SEO+GEO scores, E-E-A-T advisories, SERP feature guidance |
| 24 | Internal link suggestions panel |
| 25 | Plagiarism (Copyscape) |
| 26 | Google Analytics 4 |
| 27 | GSC integration |
| 28–31 | WP plugin, Chrome ext, Docs add-on, Public API — **not built** → Integrations above |
