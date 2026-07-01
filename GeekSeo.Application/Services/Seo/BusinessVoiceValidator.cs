using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class BusinessVoiceValidator
{
    public static readonly string[] KnownToolTokens =
    [
        "shopify",
        "hubspot",
        "quickbooks",
        "wordpress",
        "postgres",
        "postgresql",
        "react",
        "node.js",
        "nodejs",
        "zapier",
        "salesforce",
        "mailchimp",
        "stripe",
        "gohighlevel",
        "crm",
        "llm",
        "openai",
    ];

    public sealed record GateResult(string GateId, bool Passed, string Detail);

    public static IReadOnlyList<GateResult> Evaluate(string html, BusinessVoicePack pack)
    {
        if (!pack.Enabled)
            return [];

        var plain = StripTags(html);
        var results = new List<GateResult>
        {
            EvaluateConcreteExamples(plain, pack),
        };

        if (pack.RequiresTraditionalVsAiContrast)
            results.Add(EvaluateTraditionalVsAiContrast(plain));

        if (pack.RequiresCapabilityBridge)
            results.Add(EvaluateCapabilityBridge(plain, pack));

        results.Add(EvaluateCta(html, pack));
        return results;
    }

    public static bool PassesAllGates(string html, BusinessVoicePack pack) =>
        !pack.Enabled || Evaluate(html, pack).All(r => r.Passed);

    public static IReadOnlyList<GateResult> FailedGates(string html, BusinessVoicePack pack) =>
        Evaluate(html, pack).Where(r => !r.Passed).ToList();

    private static GateResult EvaluateConcreteExamples(string plain, BusinessVoicePack pack)
    {
        var count = CountConcreteExamples(plain, pack.SuggestedToolExamples);
        var passed = count >= pack.MinimumConcreteExamples;
        return new GateResult(
            "concrete_examples",
            passed,
            passed
                ? $"{count} named-tool examples found (minimum {pack.MinimumConcreteExamples})."
                : $"Only {count} named-tool examples found; need at least {pack.MinimumConcreteExamples} (e.g. {string.Join(", ", pack.SuggestedToolExamples.Take(4))}).");
    }

    private static GateResult EvaluateTraditionalVsAiContrast(string plain)
    {
        var lower = plain.ToLowerInvariant();
        var hasTraditional = TraditionalSignals.Any(signal => lower.Contains(signal, StringComparison.Ordinal));
        var hasAiWay = AiWaySignals.Any(signal => lower.Contains(signal, StringComparison.Ordinal));
        var passed = hasTraditional && hasAiWay;
        return new GateResult(
            "traditional_vs_ai",
            passed,
            passed
                ? "Traditional vs. AI contrast detected."
                : "Add an explicit old-way vs. AI-way contrast (e.g. whiteboard personas vs. clustering live support transcripts).");
    }

    private static GateResult EvaluateCapabilityBridge(string plain, BusinessVoicePack pack)
    {
        var lower = $" {plain.ToLowerInvariant()} ";
        var hasImplementLanguage = ImplementSignals.Any(signal => lower.Contains(signal, StringComparison.Ordinal));
        var mentionsCapability = pack.DeclaredCapabilities.Any(cap =>
            CapabilityMentioned(lower, cap));
        var passed = hasImplementLanguage && mentionsCapability;
        return new GateResult(
            "capability_bridge",
            passed,
            passed
                ? "Implementation capability bridge detected."
                : $"Tie the topic to how {pack.SiteName} implements {string.Join(", ", pack.DeclaredCapabilities.Take(3))} for clients.");
    }

    private static bool CapabilityMentioned(string lowerSpaced, string capability)
    {
        if (lowerSpaced.Contains(capability.ToLowerInvariant(), StringComparison.Ordinal))
            return true;

        return capability.ToLowerInvariant() switch
        {
            var c when c.Contains("chatbot") => lowerSpaced.Contains("chatbot", StringComparison.Ordinal),
            var c when c.Contains("react") => lowerSpaced.Contains("react", StringComparison.Ordinal),
            var c when c.Contains("node") => lowerSpaced.Contains("node", StringComparison.Ordinal),
            var c when c.Contains("postgres") => lowerSpaced.Contains("postgres", StringComparison.Ordinal),
            var c when c.Contains("automation") => lowerSpaced.Contains("automation", StringComparison.Ordinal) || lowerSpaced.Contains("zapier", StringComparison.Ordinal),
            var c when c.Contains("analytics") => lowerSpaced.Contains("analytics", StringComparison.Ordinal) || lowerSpaced.Contains("dashboard", StringComparison.Ordinal),
            var c when c.Contains("wordpress") => lowerSpaced.Contains("wordpress", StringComparison.Ordinal),
            var c when c.Contains(".net") => lowerSpaced.Contains(".net", StringComparison.Ordinal) || lowerSpaced.Contains("c#", StringComparison.Ordinal),
            _ => false,
        };
    }

    private static GateResult EvaluateCta(string html, BusinessVoicePack pack)
    {
        var passed = html.Contains("free strategy call", StringComparison.OrdinalIgnoreCase)
            || html.Contains(pack.CtaParagraphHtml, StringComparison.Ordinal);
        return new GateResult(
            "content_cta",
            passed,
            passed
                ? "Topic-specific CTA present."
                : "Add the required CTA paragraph before the FAQ section.");
    }

    public static int CountConcreteExamples(string plain, IReadOnlyList<string> suggestedTools)
    {
        var lower = plain.ToLowerInvariant();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tool in KnownToolTokens.Concat(suggestedTools))
        {
            if (string.IsNullOrWhiteSpace(tool))
                continue;

            if (lower.Contains(tool.Trim().ToLowerInvariant(), StringComparison.Ordinal))
                seen.Add(tool.Trim());
        }

        return seen.Count;
    }

    private static string StripTags(string html) => HtmlTagRegex().Replace(html, " ");

    private static readonly string[] TraditionalSignals =
    [
        "traditional",
        "whiteboard",
        "sticky note",
        "sticky-note",
        "workshop",
        "static persona",
        "on a hunch",
        "years ago",
        "old way",
    ];

    private static readonly string[] AiWaySignals =
    [
        "live data",
        "real-time",
        "real time",
        "transcript",
        "support ticket",
        "crm",
        "cluster",
        "llm",
        "ai way",
        "continuously",
        "in real time",
    ];

    private static readonly string[] ImplementSignals =
    [
        "we build",
        "we deploy",
        "we implement",
        "we integrate",
        "custom dashboard",
        "custom app",
        "our team",
        "implementation",
        "architect",
        " deploy ",
        " integrate",
        " build ",
        "design and deploy",
    ];

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
