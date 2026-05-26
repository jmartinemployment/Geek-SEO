namespace GeekSeoBackend.Infrastructure;

/// <summary>
/// GeekSeoBackend never calls GeekRepository directly. Persistence goes through GeekAPI internal routes only.
/// Only GeekAPI holds <c>REPO_URL</c>.
/// </summary>
public static class GeekDataGateway
{
    public const string HttpClientName = "GeekDataGateway";
    public const string TokenHttpClientName = "GeekDataGatewayToken";

    /// <summary>Maps former <c>repo/seo/*</c> GeekRepository paths to GeekAPI internal gateway.</summary>
    public const string SeoInternalPrefix = "api/seo/internal/";
}
