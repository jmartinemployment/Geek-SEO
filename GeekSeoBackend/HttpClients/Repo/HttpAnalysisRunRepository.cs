using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpAnalysisRunRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : IAnalysisRunRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<IReadOnlyList<AnalysisRunSummary>>> ListByProjectAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/analysis-runs?projectId={projectId}&userId={user.UserId}",
            ct);

        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<AnalysisRunSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));

        var value = await res.Content.ReadFromJsonAsync<List<AnalysisRunSummary>>(Json, ct);
        return Result<IReadOnlyList<AnalysisRunSummary>>.Success(value ?? []);
    }

    public async Task<Result<ContentWriterSerpExport>> GetContentWriterExportAsync(
        Guid runId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/analysis-runs/{runId}/content-writer-export?userId={user.UserId}",
            ct);

        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<ContentWriterSerpExport>.NotFound("Analysis run not found");

        if (!res.IsSuccessStatusCode)
            return Result<ContentWriterSerpExport>.Failure(await res.Content.ReadAsStringAsync(ct));

        var value = await res.Content.ReadFromJsonAsync<ContentWriterSerpExport>(Json, ct);
        return value is null
            ? Result<ContentWriterSerpExport>.Failure("Empty analysis run export response")
            : Result<ContentWriterSerpExport>.Success(value);
    }
}
