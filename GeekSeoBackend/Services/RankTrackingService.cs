namespace GeekSeoBackend.Services;

using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;

public class RankTrackingService
{
    private readonly IRankTrackingRepository _repository;
    private readonly IRankSnapshotProvider _provider;
    private readonly IProjectRepository _projects;
    private readonly ICurrentUserContext _userContext;
    private readonly ILogger<RankTrackingService> _logger;

    public RankTrackingService(
        IRankTrackingRepository repository,
        IRankSnapshotProvider provider,
        IProjectRepository projects,
        ICurrentUserContext userContext,
        ILogger<RankTrackingService> logger)
    {
        _repository = repository;
        _provider = provider;
        _projects = projects;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SeoTrackedKeyword>>> GetTrackedKeywordsAsync(
        Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var projectResult = await _projects.GetByIdAsync(projectId, _userContext.UserId, ct);
            if (!projectResult.IsSuccess)
                return Result<IReadOnlyList<SeoTrackedKeyword>>.Failure(projectResult.Error!);

            return await _repository.GetKeywordsAsync(projectId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tracked keywords for project {ProjectId}", projectId);
            return Result<IReadOnlyList<SeoTrackedKeyword>>.Failure(ex.Message);
        }
    }

    public async Task<Result<SeoTrackedKeyword>> AddTrackedKeywordAsync(
        Guid projectId, TrackedKeywordRequest request, CancellationToken ct = default)
    {
        try
        {
            var projectResult = await _projects.GetByIdAsync(projectId, _userContext.UserId, ct);
            if (!projectResult.IsSuccess)
                return Result<SeoTrackedKeyword>.Failure(projectResult.Error!);

            var entity = new SeoTrackedKeyword
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Keyword = request.Keyword,
                Location = request.Location,
                Device = request.Device,
                Enabled = true,
                AddedAt = DateTimeOffset.UtcNow
            };

            return await _repository.AddKeywordAsync(entity, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tracked keyword for project {ProjectId}", projectId);
            return Result<SeoTrackedKeyword>.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteTrackedKeywordAsync(Guid keywordId, CancellationToken ct = default)
    {
        try
        {
            return await _repository.DeleteKeywordAsync(keywordId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tracked keyword {KeywordId}", keywordId);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<RankHistoryPoint>>> GetRankHistoryAsync(
        Guid projectId, string keyword, int days, CancellationToken ct = default)
    {
        try
        {
            var projectResult = await _projects.GetByIdAsync(projectId, _userContext.UserId, ct);
            if (!projectResult.IsSuccess)
                return Result<IReadOnlyList<RankHistoryPoint>>.Failure(projectResult.Error!);

            var historyResult = await _repository.GetHistoryAsync(projectId, keyword, days, ct);
            if (!historyResult.IsSuccess)
                return Result<IReadOnlyList<RankHistoryPoint>>.Failure(historyResult.Error!);

            var points = historyResult.Value!
                .Select(r => new RankHistoryPoint
                {
                    Date = r.Date,
                    Position = r.Position,
                    PageUrl = r.PageUrl
                })
                .ToList();

            return Result<IReadOnlyList<RankHistoryPoint>>.Success(points);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rank history for project {ProjectId} keyword {Keyword}", projectId, keyword);
            return Result<IReadOnlyList<RankHistoryPoint>>.Failure(ex.Message);
        }
    }

    public async Task<Result> SnapshotProjectRanksAsync(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var projectResult = await _projects.GetByIdAsync(projectId, _userContext.UserId, ct);
            if (!projectResult.IsSuccess)
                return Result.Failure(projectResult.Error);

            var project = projectResult.Value!;
            var domain = ExtractDomain(project.Url);

            var keywordsResult = await _repository.GetKeywordsAsync(projectId, ct);
            if (!keywordsResult.IsSuccess)
                return Result.Failure(keywordsResult.Error);

            var keywords = keywordsResult.Value!
                .Where(k => k.Enabled)
                .ToList();

            foreach (var keyword in keywords)
            {
                try
                {
                    var rankResult = await _provider.GetRankAsync(
                        keyword.Keyword, domain, keyword.Location, ct);

                    if (rankResult.IsSuccess && rankResult.Value != null)
                    {
                        var snapshot = new SeoRankTracking
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = projectId,
                            Keyword = keyword.Keyword,
                            Date = DateOnly.FromDateTime(DateTime.UtcNow),
                            Position = rankResult.Value.Position,
                            PageUrl = rankResult.Value.PageUrl
                        };

                        await _repository.UpsertSnapshotAsync(snapshot, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to snapshot rank for keyword {Keyword}", keyword.Keyword);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error snapshotting ranks for project {ProjectId}", projectId);
            return Result.Failure(ex.Message);
        }
    }

    private static string ExtractDomain(string url)
    {
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }
}
