using System.Text.Json;
using Npgsql;
using SiteAnalyzer2.Services.CompetitorCrawl;

namespace SiteAnalyzer2.Api.Realtime;

public sealed class PostgresCompetitorCrawlNotifier(string connectionString)
{
    public const string Channel = "sa2_competitor_crawl_progress";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task NotifyRawAsync(string payload, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT pg_notify(@channel, @payload)", connection);
        command.Parameters.AddWithValue("channel", Channel);
        command.Parameters.AddWithValue("payload", payload);
        await command.ExecuteNonQueryAsync(ct);
    }

    public Task NotifyAsync(CompetitorCrawlProgressEvent progress, CancellationToken ct = default) =>
        NotifyRawAsync(JsonSerializer.Serialize(progress, JsonOptions), ct);

    public static CompetitorCrawlProgressEvent? Deserialize(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        return JsonSerializer.Deserialize<CompetitorCrawlProgressEvent>(payload, JsonOptions);
    }
}
