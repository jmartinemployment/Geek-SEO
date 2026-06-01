namespace GeekSeo.Application.Interfaces;

/// <summary>
/// Sets the effective user id for repository calls when no HTTP request is active (background jobs).
/// </summary>
public interface IBackgroundUserContext
{
    void SetUserId(Guid userId);
}
