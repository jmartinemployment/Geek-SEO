# ADR 005: Bounded PageRank

## Status
Accepted

## Scopes
- **TargetInternal**: all `internal_links` on target site
- **SerpSet**: only `cross_run_links` where `is_internal_to_domain = false`

Intra-domain competitor self-links stored for audit, excluded from SerpSet PageRank to prevent self-link dominance.

## Algorithm
Standard PageRank on bounded node set (damping 0.85, 20 iterations).
