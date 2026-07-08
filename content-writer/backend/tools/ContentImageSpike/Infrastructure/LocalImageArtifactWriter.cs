using ContentImageSpike.Abstractions;
using ContentImageSpike.Domain;
using Microsoft.Extensions.Options;

namespace ContentImageSpike.Infrastructure;

public sealed class LocalImageArtifactWriter : IImageArtifactWriter
{
    private readonly ImageSpikeOptions _options;

    public LocalImageArtifactWriter(IOptions<ImageSpikeOptions> options) => _options = options.Value;

    public async Task<string> WriteAsync(
        Guid projectId,
        ImageGenerationResult result,
        CancellationToken cancellationToken = default)
    {
        var extension = result.ContentType switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".png",
        };

        var useCaseSlug = result.UseCase switch
        {
            ImageUseCase.PillarFigure => "pillar-figure",
            ImageUseCase.SocialFacebook => "social-facebook",
            ImageUseCase.SocialLinkedIn => "social-linkedin",
            _ => result.UseCase.ToString().ToLowerInvariant(),
        };

        var directory = Path.Combine(
            Path.GetFullPath(_options.OutputDirectory),
            projectId.ToString("N"));

        Directory.CreateDirectory(directory);

        var fileName = $"{result.ProviderId}_{useCaseSlug}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
        var path = Path.Combine(directory, fileName);

        await File.WriteAllBytesAsync(path, result.ImageBytes, cancellationToken);

        var metaPath = Path.ChangeExtension(path, ".meta.txt");
        var meta = $"""
            provider={result.ProviderId}
            useCase={result.UseCase}
            contentType={result.ContentType}
            durationMs={result.Duration.TotalMilliseconds:F0}
            remoteUrl={result.RemoteUrl ?? "(none)"}
            savedAtUtc={DateTime.UtcNow:O}
            """;
        await File.WriteAllTextAsync(metaPath, meta, cancellationToken);

        return path;
    }
}
