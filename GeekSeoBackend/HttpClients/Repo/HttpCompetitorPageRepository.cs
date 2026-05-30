using System.Net.Http.Json;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpCompetitorPageRepository(IHttpClientFactory factory) : ICompetitorPageRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<IReadOnlyList<SeoCompetitorPage>>> GetBySerpResultAsync(
        Guid serpResultId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/competitor-pages?serpResultId={serpResultId}", ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoCompetitorPage>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var list = await response.Content.ReadFromJsonAsync<List<SeoCompetitorPage>>(ct);
        return Result<IReadOnlyList<SeoCompetitorPage>>.Success(list ?? []);
    }

    public async Task<Result<SeoCompetitorPage>> UpsertAsync(
        Guid serpResultId, PageContent page, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/seo/internal/competitor-pages",
            new { serpResultId, page },
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoCompetitorPage>.Failure(await response.Content.ReadAsStringAsync(ct));
        var row = await response.Content.ReadFromJsonAsync<SeoCompetitorPage>(ct);
        return row is null
            ? Result<SeoCompetitorPage>.Failure("Empty competitor page response")
            : Result<SeoCompetitorPage>.Success(row);
    }
}
