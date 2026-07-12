namespace ContentWriter.Application.Services.Figures;

public sealed class SiteStaticImagePublisher
{
    private readonly SiteImageStorageOptions _options;

    public SiteStaticImagePublisher(SiteImageStorageOptions options)
    {
        _options = options;
    }

    public async Task<(string RelativePath, string PublicUrl)> PublishWebpAsync(
        string relativePath,
        byte[] webpBytes,
        CancellationToken cancellationToken = default)
    {
        if (webpBytes.Length == 0)
        {
            throw new InvalidOperationException("Cannot publish an empty image.");
        }

        var normalizedRelative = relativePath.TrimStart('/').Replace('\\', '/');
        var outputRoot = _options.ResolveLocalOutputRoot();
        var absolutePath = Path.Combine(outputRoot, normalizedRelative.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(absolutePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException($"Invalid image path: {relativePath}");
        }

        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(absolutePath, webpBytes, cancellationToken);

        return (normalizedRelative, _options.BuildPublicUrl(normalizedRelative));
    }
}
