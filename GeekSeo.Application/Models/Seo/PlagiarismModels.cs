namespace GeekSeo.Application.Models.Seo;

public sealed record PlagiarismStatus(bool Configured, string Provider);

public sealed record PlagiarismCheckRequest(Guid DocumentId, bool ForceRefresh = false);

public sealed record PlagiarismMatch(
    string Url,
    string? Title,
    decimal MatchPercent,
    int WordsMatched,
    string? ViewUrl);

public sealed record PlagiarismCheckResult(
    Guid Id,
    Guid DocumentId,
    decimal MatchPercent,
    bool PublishBlocked,
    bool Cached,
    DateTimeOffset CheckedAt,
    IReadOnlyList<PlagiarismMatch> Matches);
