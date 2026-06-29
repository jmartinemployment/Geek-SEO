# ADR 003: Relevance Filter

## Status
Accepted

## What Filter does and does not do

**Does:** Assign each organic `serp_item` a `FilterStatus` (`Included`, `Excluded`, `PendingReview`) using deterministic rules so downstream stages know which SERP URLs may be fetched.

**Does not:** Prove a URL is a **business competitor**. Filter runs **before** target-site Extract and before `business-profile` exists. It cannot compare SERP domains to “what the target site sells.” Included rows are **potential co-rankers for the keyword**, not validated competitors.

Excludes the run’s **target domain** (from `analysis_runs.TargetSiteUrl`) and project-owned domains. Wikipedia and reference domains may be excluded when `IncludeReferenceDomains` is false — in Geek-SEO, similar SERP rows may still be useful as **cite sources**, not crawl targets.

See [OPERATOR-WORKFLOW.md](../OPERATOR-WORKFLOW.md) and [INTEGRATIONS.md](../INTEGRATIONS.md).

## Four-bucket precedence (first match wins)
1. Auto-exclude (reference domains, owned domains, `/wiki/`)
2. Known-platform include (Reddit, Quora, YouTube, forums)
3. Commercial/competitive include (seeds, schema, cascade)
4. Pending review → manual approval sets `Included` + `ManualOverride`

## Phase 2 verification keyword
Default: **`best crm software`** — high-volume commercial query; Wikipedia reliably in top 20.
Override: env `VERIFICATION_KEYWORD`.

## Fixture matrix (`tests/fixtures/serp/`)
| Scenario | Expected |
|----------|----------|
| Wikipedia | Excluded, reference reason |
| Reddit thread | Included, KnownPlatform |
| Quora answer | Included, KnownPlatform |
| Same-domain cascade | Included, MultiPropertyCascade |
| support.competitor.com | PendingReview |
| project_owned_domains | Excluded |
