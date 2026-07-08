using ContentImageSpike.Domain;

namespace ContentImageSpike.Abstractions;

public interface IImageArtifactWriter
{
    Task<string> WriteAsync(
        Guid projectId,
        ImageGenerationResult result,
        CancellationToken cancellationToken = default);
}
