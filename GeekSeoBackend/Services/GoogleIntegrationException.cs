namespace GeekSeoBackend.Services;

public sealed class GoogleIntegrationException(string message, int statusCode = StatusCodes.Status400BadRequest)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
