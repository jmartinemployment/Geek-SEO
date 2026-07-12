namespace ContentWriter.Application.Services.Figures;

public sealed class SiteStaticImagePublisher
{
    private readonly SiteImageStorageOptions _options;

    public SiteStaticImagePublisher(SiteImageStorageOptions options)
    {
        _options = options;
    }

    public async Task<(string RelativePath, string PublicUrl)> PublishStaticImageAsync(
        string relativePath,
        byte[] imageBytes,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes.Length == 0)
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
        await File.WriteAllBytesAsync(absolutePath, imageBytes, cancellationToken);

        return (normalizedRelative, _options.BuildPublicUrl(normalizedRelative));
    }
}
