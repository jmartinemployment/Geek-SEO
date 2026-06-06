namespace GeekSeoBackend.Services.NicheExtraction;

internal static class AnchorTextFilter
{
    private static readonly HashSet<string> GenericAnchors = new(StringComparer.OrdinalIgnoreCase)
    {
        "home", "here", "more", "click here", "click", "link", "read more", "learn more",
        "view all", "see all", "see more", "view more", "continue", "next", "previous",
        "back", "menu", "close", "open", "submit", "send", "go", "skip", "details",
        "contact", "contact us", "about", "about us", "login", "log in", "sign in",
        "sign up", "register", "subscribe", "download", "get started", "get a quote",
        "free quote", "call now", "call us", "email us", "privacy", "terms",
    };

    internal static bool IsUsableTopic(string anchorText)
    {
        var trimmed = anchorText.Trim();
        if (trimmed.Length < 4 || trimmed.Length > 80)
            return false;

        if (GenericAnchors.Contains(trimmed))
            return false;

        if (trimmed.All(char.IsDigit))
            return false;

        if (trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
