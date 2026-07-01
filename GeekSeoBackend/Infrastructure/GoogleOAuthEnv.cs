namespace GeekSeoBackend.Infrastructure;

/// <summary>
/// Railway uses GOOGLE_OAUTH_CLIENT_ID; code and docs historically used GOOGLE_CLIENT_ID.
/// Accept both so production startup and GSC/GA4 OAuth work without duplicate env vars.
/// </summary>
public static class GoogleOAuthEnv
{
    public static string ClientId => First("GOOGLE_CLIENT_ID", "GOOGLE_OAUTH_CLIENT_ID");

    public static string ClientSecret => First("GOOGLE_CLIENT_SECRET");

    public static string RedirectUri => First("GOOGLE_REDIRECT_URI");

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(RedirectUri);

    public static void EnsureConfigured()
    {
        if (IsConfigured)
            return;

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(ClientId))
            missing.Add("GOOGLE_CLIENT_ID or GOOGLE_OAUTH_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(ClientSecret))
            missing.Add("GOOGLE_CLIENT_SECRET");
        if (string.IsNullOrWhiteSpace(RedirectUri))
            missing.Add("GOOGLE_REDIRECT_URI");

        throw new InvalidOperationException(
            $"Missing required Google OAuth environment variables: {string.Join(", ", missing)}");
    }

    private static string First(params string[] names)
    {
        foreach (var name in names)
        {
            var value = Environment.GetEnvironmentVariable(name)?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }
}
