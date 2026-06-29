using System.Buffers;
using System.Text;
using System.Text.Json;

namespace SiteAnalyzer2.Api.Realtime;

public static class CompetitorCrawlPayloadParser
{
    public static bool TryGetRunId(string? json, out Guid runId) =>
        TryGetRunId(json.AsSpan(), out runId);

    public static bool TryGetRunId(ReadOnlySpan<char> json, out Guid runId)
    {
        runId = default;
        if (json.IsEmpty)
            return false;

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(json.Length);
        if (maxByteCount <= 1024)
        {
            Span<byte> utf8 = stackalloc byte[maxByteCount];
            var written = Encoding.UTF8.GetBytes(json, utf8);
            return TryGetRunIdFromUtf8(utf8[..written], out runId);
        }

        var rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
        try
        {
            var written = Encoding.UTF8.GetBytes(json, rented.AsSpan(0, maxByteCount));
            return TryGetRunIdFromUtf8(rented.AsSpan(0, written), out runId);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static bool TryGetRunIdFromUtf8(ReadOnlySpan<byte> utf8Json, out Guid runId)
    {
        runId = default;
        var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (!reader.ValueTextEquals("runId"u8))
                continue;

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                return false;

            return reader.TryGetGuid(out runId);
        }

        return false;
    }
}
