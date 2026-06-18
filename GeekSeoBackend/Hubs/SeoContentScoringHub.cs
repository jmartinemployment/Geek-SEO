using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.SignalR;

namespace GeekSeoBackend.Hubs;

public sealed class SeoContentScoringHub(
    IContentScoringService scoring,
    IContentDocumentService documents,
    IProjectService projects,
    IUrlResearchService urlResearch,
    INicheProfileRepository nicheProfiles,
    HubGroupAccessCache accessCache,
    IHttpContextAccessor httpContext,
    WorkerUserContext workerUser,
    ILogger<SeoContentScoringHub> logger) : Hub
{
    public async Task JoinDocument(string documentId)
    {
        if (!Guid.TryParse(documentId, out var docId)) return;
        var userId = GetUserId();
        if (userId == Guid.Empty || !await HasDocumentAccessAsync(userId, docId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, DocumentGroup(docId));
    }

    public async Task LeaveDocument(string documentId)
    {
        if (!Guid.TryParse(documentId, out var docId)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DocumentGroup(docId));
    }

    public async Task JoinNicheProfile(string profileId)
    {
        if (!Guid.TryParse(profileId, out var id)) return;
        var userId = GetUserId();
        if (userId == Guid.Empty || !await HasNicheProfileAccessAsync(userId, id)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, NicheProfileGroup(id));
    }

    public async Task LeaveNicheProfile(string profileId)
    {
        if (!Guid.TryParse(profileId, out var id)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NicheProfileGroup(id));
    }

    public async Task JoinUrlResearchProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out var id)) return;
        var userId = GetUserId();
        if (userId == Guid.Empty || !await HasProjectAccessAsync(userId, id)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, UrlResearchProjectGroup(id));
    }

    public async Task LeaveUrlResearchProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out var id)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UrlResearchProjectGroup(id));
    }

    public async Task JoinUrlResearch(string urlResearchId)
    {
        if (!Guid.TryParse(urlResearchId, out var id)) return;
        var userId = GetUserId();
        if (userId == Guid.Empty || !await HasUrlResearchAccessAsync(userId, id)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, UrlResearchGroup(id));
    }

    public async Task LeaveUrlResearch(string urlResearchId)
    {
        if (!Guid.TryParse(urlResearchId, out var id)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UrlResearchGroup(id));
    }

    /// <summary>Deprecated — use typed Join* methods. Prefix-only guard retained for legacy clients.</summary>
    [Obsolete("Use typed Join* hub methods with server-side ACL checks.")]
    public async Task JoinGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || groupName.Length > 128)
            return;

        if (groupName.StartsWith("niche-", StringComparison.Ordinal)
            && Guid.TryParse(groupName["niche-".Length..], out var profileId))
        {
            await JoinNicheProfile(profileId.ToString());
            return;
        }

        if (groupName.StartsWith("doc:", StringComparison.Ordinal)
            && Guid.TryParse(groupName["doc:".Length..], out var docId))
        {
            await JoinDocument(docId.ToString());
            return;
        }

        if (groupName.StartsWith("url-research-project-", StringComparison.Ordinal)
            && Guid.TryParse(groupName["url-research-project-".Length..], out var projectId))
        {
            await JoinUrlResearchProject(projectId.ToString());
            return;
        }

        if (groupName.StartsWith("url-research-", StringComparison.Ordinal)
            && Guid.TryParse(groupName["url-research-".Length..], out var researchId))
        {
            await JoinUrlResearch(researchId.ToString());
        }
    }

    public async Task RequestScore(string documentId)
    {
        if (!Guid.TryParse(documentId, out var docId))
            return;

        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            await Clients.Caller.SendAsync("ScoreError", new { message = "Not authenticated. Set NEXT_PUBLIC_DEV_USER_ID or sign in." });
            return;
        }

        await RunAsUserAsync(userId, async () =>
        {
            var result = await scoring.ScoreSavedDocumentAsync(userId, docId);
            await SendScoreResultAsync(documentId, result);
        });
    }

    public async Task ContentChanged(string documentId, string contentHtml, string targetKeyword)
    {
        if (!Guid.TryParse(documentId, out var docId))
            return;

        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            await Clients.Caller.SendAsync("ScoreError", new { message = "Not authenticated. Set NEXT_PUBLIC_DEV_USER_ID or sign in." });
            return;
        }

        await RunAsUserAsync(userId, async () =>
        {
            var result = await scoring.ProcessContentChangedAsync(userId, docId, contentHtml, targetKeyword);
            await SendScoreResultAsync(documentId, result);
        });
    }

    public async Task KeywordChanged(
        string documentId,
        string newKeyword,
        string location,
        string contentHtml)
    {
        if (!Guid.TryParse(documentId, out var docId))
            return;

        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            await Clients.Caller.SendAsync("ScoreError", new { message = "Not authenticated." });
            return;
        }

        await RunAsUserAsync(userId, async () =>
        {
            await Clients.Caller.SendAsync("BenchmarkRefreshing", new { keyword = newKeyword, location });

            var result = await scoring.ProcessKeywordChangedAsync(
                userId, docId, contentHtml, newKeyword, location);

            await SendScoreResultAsync(documentId, result);
        });
    }

    private async Task SendScoreResultAsync(string documentId, Result<ContentScoreHubResult> result)
    {
        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("ScoreError", new { message = result.Error });
            return;
        }

        if (result.Value?.PendingReason is not null)
        {
            await Clients.Caller.SendAsync("ScorePending", new { reason = result.Value.PendingReason });
            return;
        }

        if (result.Value?.ScoreUpdate is not null)
            await Clients.Group($"doc:{documentId}").SendAsync("ScoreUpdate", result.Value.ScoreUpdate);
    }

    private async Task RunAsUserAsync(Guid userId, Func<Task> action)
    {
        var previous = workerUser.UserId;
        workerUser.SetUserId(userId);
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scoring hub failed for user {UserId}", userId);
            await Clients.Caller.SendAsync("ScoreError", new { message = "Scoring failed. Please try again." });
        }
        finally
        {
            workerUser.SetUserId(previous);
        }
    }

    private async Task<bool> HasDocumentAccessAsync(Guid userId, Guid documentId)
    {
        var key = HubGroupAccessCache.Key(userId, "document", documentId);
        var cached = accessCache.TryGet(key);
        if (cached is not null) return cached.Value;

        var access = await documents.EnsureAccessAsync(userId, documentId);
        var allowed = access.IsSuccess;
        accessCache.Set(key, allowed);
        return allowed;
    }

    private async Task<bool> HasProjectAccessAsync(Guid userId, Guid projectId)
    {
        var key = HubGroupAccessCache.Key(userId, "project", projectId);
        var cached = accessCache.TryGet(key);
        if (cached is not null) return cached.Value;

        var access = await projects.GetAsync(userId, projectId);
        var allowed = access.IsSuccess;
        accessCache.Set(key, allowed);
        return allowed;
    }

    private async Task<bool> HasNicheProfileAccessAsync(Guid userId, Guid profileId)
    {
        var key = HubGroupAccessCache.Key(userId, "niche", profileId);
        var cached = accessCache.TryGet(key);
        if (cached is not null) return cached.Value;

        var projectIdResult = await nicheProfiles.GetProjectIdAsync(profileId);
        if (!projectIdResult.IsSuccess || projectIdResult.Value is null)
        {
            accessCache.Set(key, false);
            return false;
        }

        var allowed = await HasProjectAccessAsync(userId, projectIdResult.Value.Value);
        accessCache.Set(key, allowed);
        return allowed;
    }

    private async Task<bool> HasUrlResearchAccessAsync(Guid userId, Guid urlResearchId)
    {
        var key = HubGroupAccessCache.Key(userId, "urlResearch", urlResearchId);
        var cached = accessCache.TryGet(key);
        if (cached is not null) return cached.Value;

        var access = await urlResearch.GetHeadAsync(userId, urlResearchId);
        var allowed = access.IsSuccess;
        accessCache.Set(key, allowed);
        return allowed;
    }

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirst("sub")?.Value ?? Context.UserIdentifier;
        if (Guid.TryParse(sub, out var id) && id != Guid.Empty)
            return id;

        var header = httpContext.HttpContext?.Request.Headers["X-User-Id"].ToString();
        if (Guid.TryParse(header, out var fromHeader))
            return fromHeader;

        var accessToken = httpContext.HttpContext?.Request.Query["access_token"].ToString();
        if (Guid.TryParse(accessToken, out var fromQuery))
            return fromQuery;

        return Guid.Empty;
    }

    public static string DocumentGroup(Guid documentId) => $"doc:{documentId}";

    public static string NicheProfileGroup(Guid profileId) => $"niche-{profileId}";

    public static string UrlResearchGroup(Guid urlResearchId) => $"url-research-{urlResearchId}";

    public static string UrlResearchProjectGroup(Guid projectId) => $"url-research-project-{projectId}";
}
