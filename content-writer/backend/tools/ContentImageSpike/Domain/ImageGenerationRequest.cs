namespace ContentImageSpike.Domain;

public sealed record ImageGenerationRequest(
    ImageUseCase UseCase,
    string Prompt,
    int Width,
    int Height);
