using System.Text.Json;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentSocialPostService(
    IContentDocumentService documents,
    IAIProvider ai) : IContentSocialPostService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<ContentSocialPostResult>> GenerateAsync(
        Guid userId,
        Guid documentId,
        GenerateSocialPostRequest request,
        CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentSocialPostResult>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        var keyword = doc.TargetKeyword?.Trim() ?? "this topic";
        var title = doc.Title?.Trim() ?? keyword;
        var blogLink = string.IsNullOrWhiteSpace(request.BlogPostSlug)
            ? null
            : $"/blog/{request.BlogPostSlug}";
        var blogTitle = request.BlogPostTitle?.Trim();

        var excerpt = ExtractExcerpt(doc.ContentHtml, 400);

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt =
                "You write social media posts for small business owners. " +
                "Respond with valid JSON only: { \"facebook\": \"...\", \"linkedin\": \"...\" }. " +
                "No markdown fences. No extra keys.",
            UserPrompt = BuildPrompt(keyword, title, blogTitle, blogLink, excerpt),
            MaxTokens = 512,
            Temperature = 0.7,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return Result<ContentSocialPostResult>.Failure(response.Error ?? "Social post generation failed");

        if (!TryParse(response.Value.Content, out var fb, out var li))
            return Result<ContentSocialPostResult>.Failure("Could not parse social post response");

        return Result<ContentSocialPostResult>.Success(new ContentSocialPostResult
        {
            FacebookPost = fb,
            LinkedInPost = li,
            Keyword = keyword,
            BlogPostSlug = request.BlogPostSlug,
        });
    }

    private static string BuildPrompt(
        string keyword,
        string articleTitle,
        string? blogTitle,
        string? blogLink,
        string excerpt)
    {
        var lines = new List<string>
        {
            $"Article keyword: {keyword}",
            $"Article title: {articleTitle}",
        };

        if (!string.IsNullOrWhiteSpace(blogTitle))
            lines.Add($"Related blog post: {blogTitle}");

        if (!string.IsNullOrWhiteSpace(blogLink))
            lines.Add($"Blog post path: {blogLink}");

        if (!string.IsNullOrWhiteSpace(excerpt))
            lines.Add($"Article excerpt: {excerpt}");

        lines.Add(string.Empty);
        lines.Add("Write two social media posts:");
        lines.Add("facebook: 2-3 sentences, conversational, engaging, ends with a question or call to action. Reference the blog post link if provided.");
        lines.Add("linkedin: 3-4 sentences, professional tone, leads with a business insight from the article, references the blog post link if provided.");

        return string.Join('\n', lines);
    }

    private static string ExtractExcerpt(string? html, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length <= maxChars ? text : text[..maxChars].TrimEnd() + "…";
    }

    private static bool TryParse(string raw, out string facebook, out string linkedIn)
    {
        facebook = linkedIn = string.Empty;
        var trimmed = raw.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start) return false;

        try
        {
            using var doc = JsonDocument.Parse(trimmed[start..(end + 1)]);
            facebook = doc.RootElement.TryGetProperty("facebook", out var fb) ? fb.GetString()?.Trim() ?? string.Empty : string.Empty;
            linkedIn = doc.RootElement.TryGetProperty("linkedin", out var li) ? li.GetString()?.Trim() ?? string.Empty : string.Empty;
            return !string.IsNullOrWhiteSpace(facebook) && !string.IsNullOrWhiteSpace(linkedIn);
        }
        catch
        {
            return false;
        }
    }
}
