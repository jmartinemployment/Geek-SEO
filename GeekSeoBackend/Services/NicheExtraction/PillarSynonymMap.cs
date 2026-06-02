namespace GeekSeoBackend.Services.NicheExtraction;

internal static class PillarSynonymMap
{
    // Groups of synonymous tokens — any two slugs sharing a group token are treated as similar
    private static readonly List<HashSet<string>> Groups =
    [
        new(StringComparer.OrdinalIgnoreCase) { "repair", "fix", "service", "maintenance", "servicing" },
        new(StringComparer.OrdinalIgnoreCase) { "removal", "remove", "elimination", "clean", "cleaning" },
        new(StringComparer.OrdinalIgnoreCase) { "setup", "install", "installation", "configure", "configuration" },
        new(StringComparer.OrdinalIgnoreCase) { "support", "help", "assistance", "consulting", "consultation" },
        new(StringComparer.OrdinalIgnoreCase) { "recovery", "restore", "restoration", "backup", "retrieve" },
        new(StringComparer.OrdinalIgnoreCase) { "network", "networking", "wifi", "internet", "connectivity" },
        new(StringComparer.OrdinalIgnoreCase) { "virus", "malware", "spyware", "ransomware", "security" },
        new(StringComparer.OrdinalIgnoreCase) { "laptop", "computer", "pc", "desktop", "workstation" },
        new(StringComparer.OrdinalIgnoreCase) { "phone", "mobile", "smartphone", "iphone", "android" },
        new(StringComparer.OrdinalIgnoreCase) { "printer", "printing", "scanner", "copier" },
    ];

    // Returns synonym group for a token, or null
    public static HashSet<string>? GetGroup(string token)
    {
        foreach (var group in Groups)
        {
            if (group.Contains(token)) return group;
        }
        return null;
    }

    // Jaccard similarity on slug tokens, with synonym expansion
    public static double Similarity(string slugA, string slugB)
    {
        var tokensA = ExpandTokens(Tokenize(slugA));
        var tokensB = ExpandTokens(Tokenize(slugB));

        var intersection = tokensA.Intersect(tokensB, StringComparer.OrdinalIgnoreCase).Count();
        var union = tokensA.Union(tokensB, StringComparer.OrdinalIgnoreCase).Count();

        return union == 0 ? 0 : (double)intersection / union;
    }

    private static IReadOnlyList<string> Tokenize(string slug) =>
        slug.Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries);

    private static HashSet<string> ExpandTokens(IReadOnlyList<string> tokens)
    {
        var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            expanded.Add(token);
            var group = GetGroup(token);
            if (group is not null)
                expanded.UnionWith(group);
        }
        return expanded;
    }
}
