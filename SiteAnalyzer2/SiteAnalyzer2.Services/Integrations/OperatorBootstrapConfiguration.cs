namespace SiteAnalyzer2.Services.Integrations;

/// <summary>
/// One-time Geek-SEO project link for the first keyword import on a new site URL.
/// Set on the Api service (e.g. Railway), not in an operator <c>.env</c> file.
/// </summary>
public static class OperatorBootstrapConfiguration
{
    public const string EnvVarName = "GEEK_SEO_PROJECT_ID";

    public static Guid? TryResolveGeekSeoProjectId()
    {
        var raw = Environment.GetEnvironmentVariable(EnvVarName)?.Trim();
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
    }
}
