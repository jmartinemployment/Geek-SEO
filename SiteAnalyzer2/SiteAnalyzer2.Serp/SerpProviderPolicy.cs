namespace SiteAnalyzer2.Serp;

/// <summary>
/// Guards which SERP provider keys are allowed for the current process and execution mode.
/// </summary>
public static class SerpProviderPolicy
{
    public const string GoogleScraperKey = "google-scraper";
    public const string FixtureKey = "fixture";
    public const string ManualHtmlKey = "manual-html";

    public static bool IsExternalExecution =>
        string.Equals(
            Environment.GetEnvironmentVariable("SERP_EXECUTION")?.Trim(),
            "external",
            StringComparison.OrdinalIgnoreCase);

    public static bool FixtureRegistrationEnabled =>
        !string.Equals(
            Environment.GetEnvironmentVariable("SERP_ALLOW_FIXTURE")?.Trim(),
            "false",
            StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowedProviderKey(string providerKey)
    {
        var key = providerKey.Trim().ToLowerInvariant();
        return key switch
        {
            GoogleScraperKey => true,
            ManualHtmlKey => true,
            FixtureKey => !IsExternalExecution && FixtureRegistrationEnabled,
            _ => false
        };
    }

    public static void EnsureResolvable(string providerKey)
    {
        if (!IsAllowedProviderKey(providerKey))
            throw new InvalidOperationException(RejectionMessage(providerKey));
    }

    public static string RejectionMessage(string providerKey) =>
        $"SERP provider '{providerKey}' is not allowed for the current SERP_EXECUTION mode.";
}
