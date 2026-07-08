using ContentImageSpike.Domain;

namespace ContentImageSpike.Abstractions;

public interface IContentImageSourceReader
{
    Task<ContentImageSource?> LoadAsync(Guid projectId, CancellationToken cancellationToken = default);
}
