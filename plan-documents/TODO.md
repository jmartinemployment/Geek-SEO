# Geek SEO — TODO (deferred work)

**All future work.** The master plan ([`geekseo-plan.md`](geekseo-plan.md)) v1 scope is **100% complete** as of June 2026.

When an item ships, update [`PROJECT_STATUS.md`](../PROJECT_STATUS.md) and check it off here.

---

## #12b topical map — polish (was last v1 open items)

| Step | Work |
|------|------|
| V2.2 | Market **opportunities** — planner/DataForSEO diff vs GSC-only gaps (`coverage: opportunity` enrichment) |
| V2.4 | Dashboard **“Do this next”** panel wired to `GET /api/seo/topical-map` `recommendations` |
| V2.5 | Playwright **E2E** — GSC test project → generate map → open gap → create document *(subset of Step 30 below)* |

**Already shipped (Jun 2026):** V2.0–V2.1, V2.3, map-page recommendations rail, competitor domains on clusters, planner `TopicClusteringService` clustering.

---

## geekseo-plan.md — implementation steps (deferred)

Steps **29–31** from [`geekseo-plan.md`](geekseo-plan.md) were not required for v1 closure; track completion here.

| Step | Work | Reference |
|------|------|-----------|
| **29** | **Billing sign-off** — `/pricing` tier cards + PayPal subscription + webhook tier sync + cancel flow; all gated routes respect live `seo_subscriptions` row. Sandbox shipped; **live charges** → P4 below. | `geekseo-plan.md` Step 29 · [`docs/PAYPAL-BILLING.md`](../docs/PAYPAL-BILLING.md) |
| **30** | **Playwright E2E — all clone flows** — OAuth login; guided wizard → publish; editor SignalR score; planner → editor; topical map (GSC test project); content guard approve; calendar drag-drop; auto-optimize undo; detect + plagiarism gate. CI per `scripts/E2E_SMOKE.md`. | `geekseo-plan.md` Step 30 · `e2e/README.md` |
| **31** | **Unit tests — scoring and gates** — all 6 SEO components, 5 GEO dimensions, `NlpExtractor`, term benchmarks, feature gate matrix, usage cap enforcement (expand beyond current 13 Vitest + partial xUnit). | `geekseo-plan.md` Step 31 |

---

## Parity gaps (waived from v1 closure)

| # | Item | Notes |
|---|------|--------|
| **6** | GPTZero-branded AI detection UI | Detection/humanize exists in editor; branding not Surfer-parity |
| **15** | WordPress REST publish — production E2E | Code shipped; operator has no WP site for live QA |
| **19** | Content Guard — WP draft push E2E | Decay scan + patch + approve in DB work; WP draft path unverified |
| **20** | Multi-LLM GEO (ChatGPT, Gemini, Perplexity) | Google AIO/organic probe + trends shipped; other LLMs not built |

---

## Integrations (#28–31)

Separate products / repos. Build order when picked up: **#31 public API** → **#28 WP plugin** → **#29 Chrome** → **#30 Google Docs**.

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

**Note:** Topical map **Phases 4–5** were superseded by **#12b v2** (GSC + SERP clustering, table/map UI) in the active plan — not this list.

| Phase | Optional future (not v2) |
|-------|---------------------------|
| 4-alt | Sitemap crawl → NLP → Claude pillar tree (original REDESIGN spec) |
| 5-alt | Site focus input + dashboard “Topical coverage %” column |
| 8-alt | Free `/tools/ai-visibility` marketing tool |

---

## P4 — Ops & go-live

| Item | Doc |
|------|-----|
| PayPal **live** (real charges) — completes **Step 29** billing sign-off | [`docs/PAYPAL-BILLING.md`](../docs/PAYPAL-BILLING.md) · Step 29 above |
| Production identity — `@geekatyourspot.com` worker + project `UserId` migration | [`PROJECT_STATUS.md`](../PROJECT_STATUS.md) § Identity |
| Dogfood + launch proof (Step 33) | `geekseo-plan.md` Step 33 |
| SignalR Redis backplane (Step 34) | `geekseo-plan.md` Step 34 |
| Step 32 production deploy sign-off | `geekseo-plan.md` Step 32 |
| WordPress dogfood for #15 / #19 QA | Blocked until staging WP exists |

---

## Post-v1 upgrade track (separate plan)

Full spec: [`UPGRADE-se-ranking-agency-serpapi.md`](UPGRADE-se-ranking-agency-serpapi.md) — SerpApi provider, rank history, agency white-label tier, SE Ranking–class features (`U1`–`U10`). **Do not mix with v1 done.**

**Data plane (long-term):** [`DATA-PROVIDER-STRATEGY.md`](DATA-PROVIDER-STRATEGY.md) — Geek-owned SERP/keyword/crawl (`GeekSerpProvider`, crawl workers) to augment/replace DataForSEO; SerpApi/Bright Data as bootstrap only. Execution ties to upgrade plan phases; not v1 scope.

---

## Security & billing (pre–public paid launch)

Tracked here until v1 code scope is closed; required before **live money**, not for “feature parity complete.”

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
