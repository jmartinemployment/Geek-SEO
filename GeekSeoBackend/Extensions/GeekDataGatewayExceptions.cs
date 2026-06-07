namespace GeekSeoBackend.Extensions;

/// <summary>
/// HttpClient.Timeout throws <see cref="TaskCanceledException"/> (a subclass of
/// <see cref="OperationCanceledException"/>). Treat gateway slowness as transient;
/// let real client disconnects propagate.
/// </summary>
internal static class GeekDataGatewayExceptions
{
    public static bool IsTransientGatewayFailure(Exception ex, CancellationToken requestCt)
    {
        if (requestCt.IsCancellationRequested)
            return false;

        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or HttpRequestException)
                return true;

            if (current is TaskCanceledException)
                return true;
        }

        return false;
    }
}
