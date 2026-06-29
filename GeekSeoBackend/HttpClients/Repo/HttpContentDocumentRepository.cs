using GeekSeoBackend.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using GeekSeoBackend.Auth;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpContentDocumentRepository(IHttpClientFactory factory, ICurrentUserContext user) : IContentDocumentRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/content/{documentId}?userId={user.UserId}", ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/content?userId={user.UserId}&projectId={projectId}", ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoContentDocument>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var list = await response.Content.ReadFromJsonAsync<List<SeoContentDocument>>(ct);
        return Result<IReadOnlyList<SeoContentDocument>>.Success(list ?? []);
    }

    public async Task<Result<SeoContentDocument>> CreateAsync(
        Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/seo/internal/content?userId={userId}", request, ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result<SeoContentDocument>> UpdateContentAsync(
        Guid documentId, UpdateContentRequest request, int wordCount, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/seo/internal/content/{documentId}/content?userId={user.UserId}", request, ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result<SeoContentDocument>> UpdateStatusAsync(
        Guid documentId, string status, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/content/{documentId}/status?userId={user.UserId}", new { status }, ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result<SeoContentDocument>> AttachUrlResearchAsync(
        Guid documentId, Guid urlResearchId, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/content/{documentId}/url-research?userId={user.UserId}",
            new { urlResearchId },
            ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result<SeoContentDocument>> AttachAnalysisRunAsync(
        Guid documentId,
        Guid analysisRunId,
        string targetKeyword,
        string serpKeyword,
        Guid siteProfileId,
        CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/content/{documentId}/analysis-run?userId={user.UserId}",
            new AttachAnalysisRunBody
            {
                AnalysisRunId = analysisRunId,
                TargetKeyword = targetKeyword,
                SerpKeyword = serpKeyword,
                SiteProfileId = siteProfileId,
            },
            ct);
        return await ReadOneAsync(response, ct);
    }

    private sealed record AttachAnalysisRunBody
    {
        public required Guid AnalysisRunId { get; init; }
        public required string TargetKeyword { get; init; }
        public required string SerpKeyword { get; init; }
        public required Guid SiteProfileId { get; init; }
    }

    public async Task<Result<SeoContentDocument>> UpdateFeaturedImageAsync(
        Guid documentId, string featuredImageUrl, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/content/{documentId}/featured-image?userId={user.UserId}",
            new { featuredImageUrl },
            ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result<SeoContentDocument>> UpdateMarketingBundleAsync(
        Guid documentId, string marketingBundleJson, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/content/{documentId}/marketing-bundle?userId={user.UserId}",
            new { marketingBundleJson },
            ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result> UpdateScoreAsync(
        Guid documentId, int score, string scoreComponentsJson, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/seo/internal/content/{documentId}/score?userId={user.UserId}",
            new { score, scoreComponentsJson },
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    public Task<Result> UpdateAiDetectionScoreAsync(Guid documentId, decimal score, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());

    public async Task<Result> DeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/seo/internal/content/{documentId}?userId={user.UserId}", ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    private static async Task<Result<SeoContentDocument>> ReadOneAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoContentDocument>.NotFound("Document not found");
        if (!response.IsSuccessStatusCode)
            return Result<SeoContentDocument>.Failure(await response.Content.ReadAsStringAsync(ct));
        var doc = await response.Content.ReadFromJsonAsync<SeoContentDocument>(ct);
        return doc is null
            ? Result<SeoContentDocument>.Failure("Empty response")
            : Result<SeoContentDocument>.Success(doc);
    }
}
