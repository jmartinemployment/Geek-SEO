using Npgsql;

namespace GeekSeo.Persistence.Data;

public static class PostgresConnectionStringNormalizer
{
    /// <summary>Converts Railway/Supabase <c>postgres://</c> URLs to Npgsql key/value format.</summary>
    public static string Normalize(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return rawValue ?? string.Empty;

        var value = rawValue.ReplaceLineEndings("").Trim().Trim('"', '\'');
        if (!value.Contains("://", StringComparison.Ordinal))
            return value;

        try
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var databaseUri))
                return value;

            if (databaseUri.Scheme is not ("postgres" or "postgresql"))
                return value;

            var userInfo = databaseUri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var database = databaseUri.AbsolutePath.Trim('/').Split('/', 2)[0];

            var connBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port > 0 ? databaseUri.Port : 5432,
                Username = username,
                Password = password,
                Database = database,
            };

            foreach (var segment in databaseUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = segment.Split('=', 2);
                if (pair.Length != 2)
                    continue;

                var key = Uri.UnescapeDataString(pair[0]);
                var val = Uri.UnescapeDataString(pair[1]);
                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse<SslMode>(val, true, out var parsedMode))
                {
                    connBuilder.SslMode = parsedMode;
                }
            }

            if (connBuilder.SslMode == SslMode.Disable
                && (databaseUri.Host.Contains("supabase", StringComparison.OrdinalIgnoreCase)
                    || databaseUri.Host.Contains("railway", StringComparison.OrdinalIgnoreCase)))
            {
                connBuilder.SslMode = SslMode.Require;
            }

            return connBuilder.ConnectionString;
        }
        catch
        {
            return value;
        }
    }
}
