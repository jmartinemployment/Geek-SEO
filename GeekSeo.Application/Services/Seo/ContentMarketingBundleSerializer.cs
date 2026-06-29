using System.Text.Json;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ContentMarketingBundleSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public static ContentMarketingBundle Parse(string? json, string fallbackKeyword)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ContentMarketingBundle { PrimaryKeyword = fallbackKeyword };

        try
        {
            var bundle = JsonSerializer.Deserialize<ContentMarketingBundle>(json, Options)
                ?? new ContentMarketingBundle();
            if (string.IsNullOrWhiteSpace(bundle.PrimaryKeyword))
                bundle.PrimaryKeyword = fallbackKeyword;
            return bundle;
        }
        catch (JsonException)
        {
            return new ContentMarketingBundle { PrimaryKeyword = fallbackKeyword };
        }
    }

    public static string Serialize(ContentMarketingBundle bundle) =>
        JsonSerializer.Serialize(bundle, Options);
}
