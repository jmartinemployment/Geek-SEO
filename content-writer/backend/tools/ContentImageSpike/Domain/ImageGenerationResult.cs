namespace ContentImageSpike.Domain;

public sealed record ImageGenerationResult(
    string ProviderId,
    ImageUseCase UseCase,
    byte[] ImageBytes,
    string ContentType,
    TimeSpan Duration,
    string? RemoteUrl = null);
