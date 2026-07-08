using ContentImageSpike.Abstractions;
using ContentImageSpike.Domain;
using Microsoft.Extensions.Logging;

namespace ContentImageSpike.Application;

public sealed class ImageSpikeService
{
    private readonly IContentImageSourceReader _reader;
    private readonly IEnumerable<IImagePromptBuilder> _promptBuilders;
    private readonly IEnumerable<IImageGenerationProvider> _providers;
    private readonly IImageArtifactWriter _writer;
    private readonly ILogger<ImageSpikeService> _logger;

    public ImageSpikeService(
        IContentImageSourceReader reader,
        IEnumerable<IImagePromptBuilder> promptBuilders,
        IEnumerable<IImageGenerationProvider> providers,
        IImageArtifactWriter writer,
        ILogger<ImageSpikeService> logger)
    {
        _reader = reader;
        _promptBuilders = promptBuilders;
        _providers = providers;
        _writer = writer;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> RunAsync(
        Guid projectId,
        IReadOnlySet<string> providerFilter,
        CancellationToken cancellationToken = default)
    {
        var source = await _reader.LoadAsync(projectId, cancellationToken);
        if (source is null)
            throw new InvalidOperationException($"Project {projectId} not found.");

        var savedPaths = new List<string>();
        var builders = _promptBuilders.Where(b => IsAvailable(b, source)).ToList();

        if (builders.Count == 0)
            throw new InvalidOperationException("No image use cases available for this project (need pillar and/or social content).");

        var activeProviders = _providers
            .Where(p => providerFilter.Contains(p.ProviderId, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (activeProviders.Count == 0)
            throw new InvalidOperationException($"No providers matched filter: {string.Join(", ", providerFilter)}");

        _logger.LogInformation(
            "Spike run for project {ProjectName} ({ProjectId}) — {UseCases} use case(s), {Providers} provider(s)",
            source.ProjectName,
            source.ProjectId,
            builders.Count,
            activeProviders.Count);

        foreach (var builder in builders)
        {
            var request = builder.Build(source);
            _logger.LogInformation("Prompt [{UseCase}]: {Prompt}", request.UseCase, Truncate(request.Prompt, 160));

            foreach (var provider in activeProviders)
            {
                try
                {
                    var result = await provider.GenerateAsync(request, cancellationToken);
                    var path = await _writer.WriteAsync(projectId, result, cancellationToken);
                    savedPaths.Add(path);
                    _logger.LogInformation("Saved {Provider}/{UseCase} → {Path}", provider.ProviderId, request.UseCase, path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Provider} failed for {UseCase}", provider.ProviderId, request.UseCase);
                }
            }
        }

        return savedPaths;
    }

    private static bool IsAvailable(IImagePromptBuilder builder, ContentImageSource source) =>
        builder.UseCase switch
        {
            ImageUseCase.PillarFigure => source.Pillar is not null,
            ImageUseCase.SocialFacebook => source.Facebook is not null,
            ImageUseCase.SocialLinkedIn => source.LinkedIn is not null,
            _ => false,
        };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
