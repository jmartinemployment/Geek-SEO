using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Providers.Seo;

namespace GeekSeoBackend.Providers.Seo;

public sealed class DataForSeoRankSnapshotProvider(IHttpClientFactory httpClientFactory) : IRankSnapshotProvider
{
    public string ProviderName => "dataforseo";

    public async Task<Result<RankSnapshot>> GetRankAsync(
        string keyword, string domain, string location, CancellationToken ct = default)
    {
        if (!DataForSeoClient.TryGetCredentials(out _, out _))
        {
            return Result<RankSnapshot>.Failure(
                "DATAFORSEO_LOGIN and DATAFORSEO_PASSWORD must be set. Sign up at https://dataforseo.com/");
        }

        var body = new[]
        {
            new
            {
                keyword = keyword,
                location_name = location,
                language_code = "en",
                device = "desktop",
                se_domain = "google.com",
            },
        };

        HttpResponseMessage response;
        try
        {
            response = await DataForSeoClient.PostJsonAsync(
                httpClientFactory,
                "/v3/serp/google/organic/live/regular",
                body,
                ct);
        }
        catch (Exception ex)
        {
            return Result<RankSnapshot>.Failure($"DataForSEO request failed: {ex.Message}");
        }

        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return Result<RankSnapshot>.Failure($"DataForSEO HTTP {(int)response.StatusCode}");

            return ParseResponse(keyword, domain, raw);
        }
    }

    private static Result<RankSnapshot> ParseResponse(string keyword, string domain, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (!DataForSeoClient.IsApiSuccess(root))
                return Result<RankSnapshot>.Failure(DataForSeoClient.ReadApiErrorMessage(root));

            if (!root.TryGetProperty("tasks", out var tasksEl))
                return Result<RankSnapshot>.Failure("No tasks in DataForSEO response");

            var tasks = tasksEl.EnumerateArray().ToList();
            if (tasks.Count == 0)
                return Result<RankSnapshot>.Failure("Empty task list from DataForSEO");

            var task = tasks[0];
            if (!task.TryGetProperty("result", out var resultEl))
                return Result<RankSnapshot>.Success(new RankSnapshot
                {
                    Keyword = keyword,
                    Position = null,
                    PageUrl = null,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow)
                });

            var result = resultEl.EnumerateArray().FirstOrDefault();
            if (result.ValueKind == JsonValueKind.Undefined)
                return Result<RankSnapshot>.Success(new RankSnapshot
                {
                    Keyword = keyword,
                    Position = null,
                    PageUrl = null,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow)
                });

            if (!result.TryGetProperty("items", out var itemsEl))
                return Result<RankSnapshot>.Success(new RankSnapshot
                {
                    Keyword = keyword,
                    Position = null,
                    PageUrl = null,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow)
                });

            foreach (var item in itemsEl.EnumerateArray())
            {
                if (!item.TryGetProperty("type", out var typeEl))
                    continue;

                var type = typeEl.GetString();
                if (type != "organic")
                    continue;

                if (!item.TryGetProperty("domain", out var domainEl) ||
                    !item.TryGetProperty("rank_group", out var rankEl) ||
                    !item.TryGetProperty("url", out var urlEl))
                    continue;

                var itemDomain = domainEl.GetString() ?? string.Empty;
                if (!itemDomain.Equals(domain, StringComparison.OrdinalIgnoreCase))
                    continue;

                var position = rankEl.GetInt32();
                var url = urlEl.GetString() ?? string.Empty;

                return Result<RankSnapshot>.Success(new RankSnapshot
                {
                    Keyword = keyword,
                    Position = position,
                    PageUrl = url,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            }

            return Result<RankSnapshot>.Success(new RankSnapshot
            {
                Keyword = keyword,
                Position = null,
                PageUrl = null,
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        }
        catch (Exception ex)
        {
            return Result<RankSnapshot>.Failure($"Failed to parse DataForSEO response: {ex.Message}");
        }
    }
}
