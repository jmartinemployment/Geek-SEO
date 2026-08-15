using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// RFC 9309 / Google REP parser shared by <see cref="SitePageCrawler"/> and
/// <see cref="GeekSeoBackend.Providers.Seo.PlaywrightCrawlerProvider"/>.
/// 4xx → allow all; 5xx or unreachable → disallow all.
/// </summary>
public sealed class RobotsRules
{
    public static RobotsRules AllowAll { get; } = new(allowAll: true, disallowAll: false, [], [], null);
    public static RobotsRules DisallowAll { get; } = new(allowAll: false, disallowAll: true, [], [], null);

    private readonly bool _allowAll;
    private readonly bool _disallowAll;
    private readonly IReadOnlyList<(bool Allow, string Pattern)> _rules;

    private RobotsRules(
        bool allowAll,
        bool disallowAll,
        IReadOnlyList<(bool Allow, string Pattern)> rules,
        IReadOnlyList<string> sitemaps,
        int? crawlDelaySeconds)
    {
        _allowAll = allowAll;
        _disallowAll = disallowAll;
        _rules = rules;
        Sitemaps = sitemaps;
        CrawlDelaySeconds = crawlDelaySeconds;
    }

    public IReadOnlyList<string> Sitemaps { get; }
    public int? CrawlDelaySeconds { get; }

    public bool IsAllowed(string path)
    {
        if (_disallowAll)
            return false;
        if (_allowAll || _rules.Count == 0)
            return true;

        var bestLen = -1;
        var bestAllow = true;
        foreach (var (allow, pattern) in _rules)
        {
            if (!RobotsTxt.PathMatches(pattern, path))
                continue;
            var len = pattern.Length;
            if (len > bestLen)
            {
                bestLen = len;
                bestAllow = allow;
            }
            else if (len == bestLen && allow)
            {
                bestAllow = true;
            }
        }

        return bestLen < 0 || bestAllow;
    }

    internal static RobotsRules FromParsed(
        IReadOnlyList<(bool Allow, string Pattern)> rules,
        IReadOnlyList<string> sitemaps,
        int? crawlDelaySeconds) =>
        new(false, false, rules, sitemaps, crawlDelaySeconds);
}

public static class RobotsTxt
{
    public const string ProductToken = "GeekSEO";

    public static RobotsRules FromStatus(int statusCode)
    {
        if (statusCode >= 500)
            return RobotsRules.DisallowAll;
        if (statusCode >= 400)
            return RobotsRules.AllowAll;
        return RobotsRules.AllowAll;
    }

    public static RobotsRules Parse(string text, string userAgentProduct = ProductToken)
    {
        var sitemaps = new List<string>();
        var groups = new List<Group>();
        Group? current = null;
        var pendingAgents = new List<string>();

        foreach (var raw in text.Split(['\r', '\n']))
        {
            var line = raw.Trim();
            var hash = line.IndexOf('#');
            if (hash >= 0)
                line = line[..hash].Trim();
            if (line.Length == 0)
                continue;

            if (StartsWithKey(line, "Sitemap:", out var sitemap))
            {
                if (sitemap.Length > 0)
                    sitemaps.Add(sitemap);
                continue;
            }

            if (StartsWithKey(line, "User-agent:", out var agent))
            {
                if (current is { RulesOrDelay: true } || (current is not null && pendingAgents.Count > 0))
                {
                    // A new UA after rules starts a new group.
                    if (current is { RulesOrDelay: true })
                    {
                        groups.Add(current);
                        current = null;
                    }
                }

                if (current is null)
                    current = new Group();
                else if (current.RulesOrDelay)
                {
                    groups.Add(current);
                    current = new Group();
                }

                current.Agents.Add(agent);
                pendingAgents = current.Agents;
                continue;
            }

            current ??= new Group { Agents = { "*" } };

            if (StartsWithKey(line, "Allow:", out var allowPath))
            {
                current.Rules.Add((true, allowPath));
                continue;
            }

            if (StartsWithKey(line, "Disallow:", out var disallowPath))
            {
                if (disallowPath.Length == 0)
                    continue;
                current.Rules.Add((false, disallowPath));
                continue;
            }

            if (StartsWithKey(line, "Crawl-delay:", out var delayRaw)
                && int.TryParse(delayRaw, out var delay)
                && delay >= 0)
            {
                current.CrawlDelaySeconds = delay;
            }
        }

        if (current is not null)
            groups.Add(current);

        var selected = SelectGroups(groups, userAgentProduct);
        if (selected.Count == 0)
            return RobotsRules.FromParsed([], sitemaps, null);

        var rules = selected.SelectMany(g => g.Rules).ToList();
        int? crawlDelay = null;
        foreach (var g in selected)
        {
            if (g.CrawlDelaySeconds is int d)
            {
                crawlDelay = crawlDelay is int existing ? Math.Max(existing, d) : d;
            }
        }

        return RobotsRules.FromParsed(rules, sitemaps, crawlDelay);
    }

    public static bool PathMatches(string pattern, string path)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        var endAnchor = pattern.EndsWith('$');
        var body = endAnchor ? pattern[..^1] : pattern;
        var escaped = Regex.Escape(body).Replace(@"\*", ".*", StringComparison.Ordinal);
        var re = "^" + escaped + (endAnchor ? "$" : "");
        return Regex.IsMatch(path, re, RegexOptions.CultureInvariant);
    }

    public static async Task<RobotsRules> FetchAsync(Uri origin, HttpClient client, CancellationToken ct)
    {
        var robotsUrl = $"{origin.GetLeftPart(UriPartial.Authority)}/robots.txt";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, robotsUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", CrawlerIdentity.UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            using var response = await client.SendAsync(request, ct);
            var status = (int)response.StatusCode;
            if (status >= 500)
                return RobotsRules.DisallowAll;
            if (status >= 400)
                return RobotsRules.AllowAll;

            var text = await response.Content.ReadAsStringAsync(ct);
            return Parse(text);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return RobotsRules.DisallowAll;
        }
    }

    private static List<Group> SelectGroups(List<Group> groups, string product)
    {
        var matched = new List<(Group Group, int Specificity)>();
        foreach (var group in groups)
        {
            var spec = 0;
            var any = false;
            foreach (var agent in group.Agents)
            {
                if (agent == "*")
                {
                    any = true;
                    continue;
                }

                if (product.Contains(agent, StringComparison.OrdinalIgnoreCase)
                    || agent.Contains(product, StringComparison.OrdinalIgnoreCase)
                    || CrawlerIdentity.UserAgent.Contains(agent, StringComparison.OrdinalIgnoreCase))
                {
                    any = true;
                    spec = Math.Max(spec, agent.Length);
                }
            }

            if (any)
                matched.Add((group, spec));
        }

        if (matched.Count == 0)
            return [];

        var max = matched.Max(m => m.Specificity);
        return matched.Where(m => m.Specificity == max).Select(m => m.Group).ToList();
    }

    private static bool StartsWithKey(string line, string key, out string value)
    {
        if (!line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
        {
            value = string.Empty;
            return false;
        }

        value = line[key.Length..].Trim();
        return true;
    }

    private sealed class Group
    {
        public List<string> Agents { get; } = [];
        public List<(bool Allow, string Pattern)> Rules { get; } = [];
        public int? CrawlDelaySeconds { get; set; }
        public bool RulesOrDelay => Rules.Count > 0 || CrawlDelaySeconds is not null;
    }
}
