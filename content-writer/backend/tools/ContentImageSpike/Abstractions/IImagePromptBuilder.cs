using ContentImageSpike.Domain;

namespace ContentImageSpike.Abstractions;

public interface IImagePromptBuilder
{
    ImageUseCase UseCase { get; }

    ImageGenerationRequest Build(ContentImageSource source);
}
