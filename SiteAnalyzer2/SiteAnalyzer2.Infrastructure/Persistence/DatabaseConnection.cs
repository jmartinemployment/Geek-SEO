using Microsoft.Extensions.Configuration;

namespace SiteAnalyzer2.Infrastructure.Persistence;

public static class DatabaseConnection
{
    public const string EnvVarName = "SITE_ANALYZER2_DATABASE_URL";
    public const string ConfigKey = "ConnectionStrings:DefaultConnection";
    public const string UserSecretsId = "siteanalyzer2-api-7f3a9c2e-4b1d-4e8a-9f6c-2d8e5a1b0c3d";

    public static string ResolveRequired(IConfiguration configuration)
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvVarName);
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return Normalize(fromEnv);

        var fromConfig = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return Normalize(fromConfig);

        throw new InvalidOperationException(
            $"{EnvVarName} is not set and {ConfigKey} is not configured. " +
            "SiteAnalyzer requires an explicit Supabase PostgreSQL connection string — there is no default or local fallback.");
    }

    /// <summary>
    /// Converts postgres/postgresql URI strings (as shown in Supabase) to Npgsql key=value format.
    /// </summary>
    public static string Normalize(string connectionString)
    {
        var trimmed = connectionString.Trim();
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var uri = new Uri(trimmed);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        if (string.IsNullOrWhiteSpace(database))
            database = "postgres";

        var port = uri.Port > 0 ? uri.Port : 5432;
        var builder = new System.Text.StringBuilder()
            .Append("Host=").Append(uri.Host)
            .Append(";Port=").Append(port)
            .Append(";Database=").Append(database)
            .Append(";Username=").Append(username)
            .Append(";Password=").Append(password)
            .Append(";SSL Mode=Require;Trust Server Certificate=true");

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = Uri.UnescapeDataString(parts[0]);
                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                    continue;
            }
        }

        return builder.ToString();
    }
}
