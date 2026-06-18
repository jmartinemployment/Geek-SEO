namespace GeekSeoBackend.Hubs;

/// <summary>Client-side hub method names pushed via IHubContext SendAsync.</summary>
public static class SeoHubClientEvents
{
    public const string DraftJobProgress = "DraftJobProgress";
    public const string DraftJobComplete = "DraftJobComplete";
}
