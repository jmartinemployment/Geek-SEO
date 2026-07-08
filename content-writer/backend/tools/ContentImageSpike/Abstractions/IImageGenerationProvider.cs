using ContentImageSpike.Domain;

namespace ContentImageSpike.Abstractions;

public interface IImageGenerationProvider
{
    string ProviderId { get; }

    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default);
}
