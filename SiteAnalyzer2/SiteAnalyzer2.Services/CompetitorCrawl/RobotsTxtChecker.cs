namespace SiteAnalyzer2.Services.CompetitorCrawl;

public sealed class RobotsTxtChecker(IHttpClientFactory httpClientFactory)
{
    private readonly Dictionary<string, RobotsRules> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> IsAllowedAsync(Uri url, CancellationToken ct = default)
    {
        var origin = $"{url.Scheme}://{url.Host}";
        if (!_cache.TryGetValue(origin, out var rules))
        {
            rules = await LoadRulesAsync(origin, ct);
            _cache[origin] = rules;
        }

        return rules.IsAllowed(url.AbsolutePath);
    }

    private async Task<RobotsRules> LoadRulesAsync(string origin, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient(nameof(RobotsTxtChecker));
            client.Timeout = TimeSpan.FromSeconds(15);
            var robotsUrl = $"{origin}/robots.txt";
            using var response = await client.GetAsync(robotsUrl, ct);
            if (!response.IsSuccessStatusCode)
                return RobotsRules.AllowAll;

            var text = await response.Content.ReadAsStringAsync(ct);
            return RobotsRules.Parse(text);
        }
        catch
        {
            return RobotsRules.AllowAll;
        }
    }

    private sealed class RobotsRules
    {
        public static RobotsRules AllowAll { get; } = new([]);

        private readonly List<string> _disallow;

        private RobotsRules(List<string> disallow) => _disallow = disallow;

        public static RobotsRules Parse(string text)
        {
            var disallow = new List<string>();
            var inStarAgent = false;
            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
                {
                    var agent = line["User-agent:".Length..].Trim();
                    inStarAgent = agent == "*";
                    continue;
                }

                if (!inStarAgent)
                    continue;

                if (line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
                {
                    var path = line["Disallow:".Length..].Trim();
                    if (path.Length > 0)
                        disallow.Add(path);
                }
            }

            return new RobotsRules(disallow);
        }

        public bool IsAllowed(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "/";

            foreach (var rule in _disallow)
            {
                if (rule == "/")
                    return false;
                if (path.StartsWith(rule, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
    }
}
