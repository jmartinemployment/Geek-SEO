using System.Text.Json;
using System.Web;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Services.LocalServiceArea;

public sealed class GoogleGeocodeService(IHttpClientFactory httpClientFactory) : IGeocodeService
{
    public async Task<Result<GeoCoordinate>> GeocodeAsync(string addressQuery, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result<GeoCoordinate>.Failure("GOOGLE_MAPS_API_KEY is not configured.");

        if (string.IsNullOrWhiteSpace(addressQuery))
            return Result<GeoCoordinate>.Failure("Address query is required.");

        var url =
            $"https://maps.googleapis.com/maps/api/geocode/json?address={HttpUtility.UrlEncode(addressQuery.Trim())}&key={apiKey}";

        var client = httpClientFactory.CreateClient("GoogleGeocode");
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return Result<GeoCoordinate>.Failure($"Google Geocoding HTTP {(int)response.StatusCode}.");

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
            return Result<GeoCoordinate>.Failure($"Google Geocoding status: {status ?? "unknown"}.");

        if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return Result<GeoCoordinate>.Failure("Google Geocoding returned no results.");

        var location = results[0].GetProperty("geometry").GetProperty("location");
        return Result<GeoCoordinate>.Success(new GeoCoordinate(
            location.GetProperty("lat").GetDouble(),
            location.GetProperty("lng").GetDouble()));
    }
}
