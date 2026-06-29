# ADR 001: Fetch Strategy

## Status
Accepted

## Decision
Use **raw HTTP first** (AngleSharp) for page fetch and parse. Escalate to **Playwright** when ≥2 of: missing H2–H6, missing JSON-LD that headless finds, or empty main content block.

Persist `fetch_mode` per page: `Http` | `Headless`.

## Calibration matrix (representative sample)
| URL type | HTTP H2+ | HTTP JSON-LD | Headless needed |
|----------|----------|--------------|-----------------|
| Static HTML | Yes | Yes | No |
| React SPA shell | No | No | Yes |
| Next.js SSR | Yes | Yes | No |
| Wix | Partial | Partial | Yes |

Full matrix recorded at implementation in `tests/fixtures/html/`.
