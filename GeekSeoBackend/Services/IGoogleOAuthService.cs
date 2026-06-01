using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public interface IGoogleOAuthService
{
    Task<GoogleConnectUrlResponse> GetConnectUrlAsync(
        Guid userId,
        Guid projectId,
        string? propertyId,
        string? siteUrl,
        CancellationToken ct = default);

    Task<GoogleCallbackOutcome> HandleCallbackAsync(
        string code,
        string state,
        CancellationToken ct = default);

    Task<GoogleIntegrationStatusResponse> GetStatusAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task DisconnectAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<string> GetGscAccessTokenAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<(string AccessToken, string PropertyId)> GetGa4AccessTokenAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default);

    /// <summary>
    /// Re-resolves the stored GSC site URL against the user's accessible Search Console properties.
    /// Updates the connection when Google returns a different canonical siteUrl string.
    /// </summary>
    Task<string?> SyncGscSiteUrlAsync(Guid userId, Guid projectId, CancellationToken ct = default);

    /// <summary>
    /// Re-resolves the stored GA4 property against accessible properties and web data streams.
    /// Updates the connection when a better property match is found for the project URL.
    /// </summary>
    Task<string?> SyncGa4PropertyAsync(Guid userId, Guid projectId, CancellationToken ct = default);
}
