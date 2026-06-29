namespace SiteAnalyzer2.Api.Contracts;

using System.Text.Json;

public record CreateProjectRequest(string Name, int? MaxCrawlDepth, int? MaxCrawlPages);

public record CreateProjectResponse(Guid Id, string Name);

public record CreateRunRequest(
    string Keyword,
    string TargetSiteUrl,
    string SerpProviderKey = "google-scraper",
    bool IncludeReferenceDomains = false);

public record CreateRunResponse(Guid Id, string Status, string? CurrentStage);

public record RunSummaryResponse(
    Guid Id,
    Guid ProjectId,
    string Keyword,
    string TargetSiteUrl,
    string Status,
    string? CurrentStage,
    string? LatestValidationMessage,
    IReadOnlyList<GateSummary> Gates);

public record GateSummary(string Stage, bool Passed, string ValidationMessage, DateTime CheckedAt);

public record AdvanceStageRequest(string Stage);

public record BusinessProfileResponse(
    string BusinessType,
    IReadOnlyList<string> PrimaryServices,
    string? ServiceArea,
    string Description,
    object GeneratedSchemaJson,
    bool HasExistingSchema,
    bool? ExistingSchemaMatches,
    DateTime LastGeneratedAt,
    Guid? ReusedFromRunId);

public record BusinessProfileNotAvailableResponse(string Error, string Reason);

public record UpsertBusinessProfileRequest(
    string BusinessType,
    IReadOnlyList<string> PrimaryServices,
    string? ServiceArea,
    string Description,
    JsonElement GeneratedSchemaJson,
    bool HasExistingSchema,
    bool? ExistingSchemaMatches = null);

public record PendingSerpRunItem(Guid Id, Guid ProjectId, string Keyword, string SerpProviderKey);

public record PendingSerpRunsResponse(IReadOnlyList<PendingSerpRunItem> Runs);

public record SerpWorkerOrganicDto(int Position, string Url, string Title, string Snippet, string Domain);

public record SerpWorkerPaaDto(int Sequence, string QuestionText);

public record SerpWorkerResultRequest(
    bool Success,
    string? Html,
    string? FailureMessage);
