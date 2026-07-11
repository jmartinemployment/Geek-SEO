namespace ContentWriter.Application.Services.Publish;

/// <summary>
/// Department slugs accepted by geekatyourspot.com use-case and blog routes.
/// </summary>
public static class SiteDepartments
{
    public static readonly IReadOnlyList<string> All =
    [
        "accounting",
        "customer-service",
        "human-resources",
        "marketing",
        "sales",
    ];

    public static bool IsKnown(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && All.Contains(slug.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string slug) =>
        All.First(d => d.Equals(slug.Trim(), StringComparison.OrdinalIgnoreCase));
}
