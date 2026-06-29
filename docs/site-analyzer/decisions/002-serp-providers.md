# ADR 002: SERP Provider Contract

## Status
**Superseded** by [008-serp-scraper.md](008-serp-scraper.md). Paid API adapters (SerpAPI, DataForSEO, ValueSERP) are removed from scope.

## Historical interface (still valid)
```csharp
public interface ISerpProvider {
    string ProviderKey { get; }
    Task<SerpResultSet> FetchOrganicResultsAsync(SerpQuery query, CancellationToken ct);
}
```

Normalized model: `position`, `url`, `title`, `snippet`, `domain`.

See ADR 008 for the live `google-scraper` implementation, pacing, PAA capture, and failure rules.
