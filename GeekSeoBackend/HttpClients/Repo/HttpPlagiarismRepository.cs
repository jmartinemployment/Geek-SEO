using System.Net;
using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpPlagiarismRepository(IHttpClientFactory factory, ICurrentUserContext user) : IPlagiarismRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoPlagiarismCheck?>> GetLatestByDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/plagiarism?documentId={documentId}&userId={user.UserId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoPlagiarismCheck?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoPlagiarismCheck?>.Failure(await response.Content.ReadAsStringAsync(ct));

        var value = await response.Content.ReadFromJsonAsync<SeoPlagiarismCheck>(ct);
        return Result<SeoPlagiarismCheck?>.Success(value);
    }

    public async Task<Result<SeoPlagiarismCheck>> CreateAsync(SeoPlagiarismCheck check, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/seo/internal/plagiarism?userId={user.UserId}",
            check,
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoPlagiarismCheck>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoPlagiarismCheck>(ct);
        return value is null
            ? Result<SeoPlagiarismCheck>.Failure("Empty response")
            : Result<SeoPlagiarismCheck>.Success(value);
    }
}
