namespace GeekSeoBackend.Services;

public interface IUrlResearchProgressNotifier
{
    Task PushAsync(
        Guid urlResearchId,
        Guid projectId,
        Guid userId,
        string status,
        string? message = null,
        string? errorMessage = null,
        CancellationToken ct = default);
}
