namespace ContentWriter.Application.Services.Figures;

using ContentWriter.Domain.Enums;

public sealed class SiteImageStorageOptions
{
    public const string SectionName = "SiteImageStorage";

    public string PublicBaseUrl { get; set; } = "https://www.geekatyourspot.com";

    /// <summary>Filesystem path to the site <c>public</c> directory (e.g. geekatyourspot/public).</summary>
    public string LocalOutputRoot { get; set; } = string.Empty;

    /// <summary>site_static (default) or vercel_blob when BLOB_READ_WRITE_TOKEN is set.</summary>
    public string DefaultStorage { get; set; } = FigureImageStorage.SiteStatic;

    public string ResolveDefaultStorage(BlobStorageOptions? blobOptions)
    {
        var fromEnv = Environment.GetEnvironmentVariable("CONTENT_IMAGE_STORAGE");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.Trim();
        }

        if (string.Equals(DefaultStorage, FigureImageStorage.VercelBlob, StringComparison.OrdinalIgnoreCase)
            && blobOptions?.IsConfigured == true)
        {
            return FigureImageStorage.VercelBlob;
        }

        return FigureImageStorage.SiteStatic;
    }

    public string ResolveLocalOutputRoot()
    {
        if (!string.IsNullOrWhiteSpace(LocalOutputRoot))
        {
            return LocalOutputRoot;
        }

        var fromEnv = Environment.GetEnvironmentVariable("CONTENT_IMAGE_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var sibling = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "../../../geekatyourspot/public"));
        if (Directory.Exists(sibling))
        {
            return sibling;
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "public"));
    }

    public string BuildPublicUrl(string relativePath)
    {
        var baseUrl = PublicBaseUrl.TrimEnd('/');
        var path = relativePath.TrimStart('/');
        return $"{baseUrl}/{path}";
    }
}
