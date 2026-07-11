using System.Text;
using ContentWriter.Application.DTOs;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentWriter.Application.Services.Export;

public interface IContentMarkdownExportService
{
    Task<ExportMarkdownResult> ExportAsync(
        Guid projectId,
        string? departmentOverride = null,
        CancellationToken cancellationToken = default);
}

public sealed record ExportedMarkdownFile(
    string ContentType,
    string RelativePath,
    string? FilePath,
    string Markdown);

public sealed record ExportMarkdownResult(string Department, IReadOnlyList<ExportedMarkdownFile> Files);

public class ContentMarkdownExportService : IContentMarkdownExportService
{
    private const int PillarBodyMinWords = 200;

    private readonly IProjectRepository _projectRepository;
    private readonly CompanyProfileOptions _companyProfile;
    private readonly ContentExportOptions _exportOptions;
    private readonly ILogger<ContentMarkdownExportService> _logger;

    public ContentMarkdownExportService(
        IProjectRepository projectRepository,
        IOptions<CompanyProfileOptions> companyProfile,
        IOptions<ContentExportOptions> exportOptions,
        ILogger<ContentMarkdownExportService> logger)
    {
        _projectRepository = projectRepository;
        _companyProfile = companyProfile.Value;
        _exportOptions = exportOptions.Value;
        _logger = logger;
    }

    public async Task<ExportMarkdownResult> ExportAsync(
        Guid projectId,
        string? departmentOverride = null,
        CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetWithDetailsAsync(projectId, cancellationToken)
            ?? throw new ContentGenerationException($"Project {projectId} was not found.");

        var contentSet = GeneratedContentSetAssembler.Assemble(
            project,
            _companyProfile.ArticleBaseUrl,
            _companyProfile.BlogBaseUrl,
            departmentOverride);

        var department = contentSet.Department;
        var slug = contentSet.ArticleSlug
            ?? contentSet.BlogSlug
            ?? DepartmentNameResolver.SanitizeDirectorySegment(project.Name);

        var topicFolder = DepartmentNameResolver.ResolveTopicFolder(project.TargetKeyword, slug);

        var outputRoot = TryResolveOutputRoot();
        var exportedAt = DateTime.UtcNow;
        var written = new List<ExportedMarkdownFile>();

        if (contentSet.Article is not null
            && contentSet.Article.WordCount >= PillarBodyMinWords
            && !string.IsNullOrWhiteSpace(contentSet.ArticleSlug))
        {
            var relativePath = Path.Combine(department, topicFolder, "Pillar", $"{contentSet.ArticleSlug}.md");
            var markdown = MarkdownExportDocumentBuilder.Build(new MarkdownExportInput(
                Title: contentSet.Article.Title,
                Slug: contentSet.ArticleSlug,
                MetaDescription: contentSet.Article.MetaDescription,
                CanonicalUrl: contentSet.ArticleUrl ?? string.Empty,
                ContentType: "pillar",
                Department: department,
                WordCount: contentSet.Article.WordCount,
                Keywords: contentSet.Article.Keywords,
                RelatedUrl: contentSet.BlogUrl,
                BodyHtml: contentSet.Article.BodyHtml,
                JsonLdSchema: contentSet.ArticleJsonLd,
                RelatedJsonLdSchema: contentSet.BlogJsonLd,
                ExportedAtUtc: exportedAt));

            written.Add(await WriteExportAsync(outputRoot, relativePath, "pillar", markdown, cancellationToken));
        }

        if (contentSet.Blog is not null
            && contentSet.Blog.WordCount > 0
            && !string.IsNullOrWhiteSpace(contentSet.BlogSlug))
        {
            var relativePath = Path.Combine(department, topicFolder, "Blog", $"{contentSet.BlogSlug}.md");
            var markdown = MarkdownExportDocumentBuilder.Build(new MarkdownExportInput(
                Title: contentSet.Blog.Title,
                Slug: contentSet.BlogSlug,
                MetaDescription: contentSet.Blog.MetaDescription,
                CanonicalUrl: contentSet.BlogUrl ?? string.Empty,
                ContentType: "blog",
                Department: department,
                WordCount: contentSet.Blog.WordCount,
                Keywords: contentSet.Blog.Keywords,
                RelatedUrl: contentSet.ArticleUrl,
                BodyHtml: contentSet.Blog.BodyHtml,
                JsonLdSchema: contentSet.BlogJsonLd,
                RelatedJsonLdSchema: contentSet.ArticleJsonLd,
                ExportedAtUtc: exportedAt));

            written.Add(await WriteExportAsync(outputRoot, relativePath, "blog", markdown, cancellationToken));
        }

        if (contentSet.FacebookPost is not null && !string.IsNullOrWhiteSpace(contentSet.FacebookPost.Text))
        {
            var relativePath = Path.Combine(department, topicFolder, "Social", $"facebook-{slug}.md");
            var markdown = MarkdownExportDocumentBuilder.BuildSocial(
                contentSet.FacebookPost, department, slug, exportedAt);
            written.Add(await WriteExportAsync(outputRoot, relativePath, "social-facebook", markdown, cancellationToken));
        }

        if (contentSet.LinkedInPost is not null && !string.IsNullOrWhiteSpace(contentSet.LinkedInPost.Text))
        {
            var relativePath = Path.Combine(department, topicFolder, "Social", $"linkedin-{slug}.md");
            var markdown = MarkdownExportDocumentBuilder.BuildSocial(
                contentSet.LinkedInPost, department, slug, exportedAt);
            written.Add(await WriteExportAsync(outputRoot, relativePath, "social-linkedin", markdown, cancellationToken));
        }

        if (contentSet.ColdOutreachEmail is not null
            && !string.IsNullOrWhiteSpace(contentSet.ColdOutreachEmail.BodyText))
        {
            var relativePath = Path.Combine(department, topicFolder, "Email", $"cold-outreach-{slug}.md");
            var markdown = MarkdownExportDocumentBuilder.BuildColdOutreach(
                contentSet.ColdOutreachEmail, department, slug, exportedAt);
            written.Add(await WriteExportAsync(outputRoot, relativePath, "email-cold-outreach", markdown, cancellationToken));
        }

        if (contentSet.ImagePrompts is not null)
        {
            foreach (var prompt in contentSet.ImagePrompts.Sections)
            {
                if (string.IsNullOrWhiteSpace(prompt.Prompt))
                    continue;

                var folder = string.Equals(prompt.SourceType, "blog", StringComparison.OrdinalIgnoreCase)
                    ? "Blog"
                    : "Pillar";
                var fileName = $"{SlugHelper.Slugify(prompt.Heading)}.md";
                var relativePath = Path.Combine(department, topicFolder, "ImagePrompts", folder, fileName);
                var markdown = MarkdownExportDocumentBuilder.BuildSectionImagePrompt(
                    prompt, department, slug, exportedAt);
                var contentType = $"image-prompt-{prompt.SourceType}";
                written.Add(await WriteExportAsync(outputRoot, relativePath, contentType, markdown, cancellationToken));
            }
        }

        if (written.Count == 0)
        {
            throw new ContentGenerationException(
                "Nothing to export. Generate pillar body, blog, social, email, and/or image prompts first.");
        }

        return new ExportMarkdownResult(department, written);
    }

    private async Task<ExportedMarkdownFile> WriteExportAsync(
        string? outputRoot,
        string relativePath,
        string contentType,
        string markdown,
        CancellationToken cancellationToken)
    {
        string? filePath = null;
        if (outputRoot is not null)
        {
            filePath = Path.Combine(outputRoot, relativePath);
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(filePath, markdown, Encoding.UTF8, cancellationToken);
                _logger.LogInformation("Exported {ContentType} markdown to {Path}", contentType, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write export file to {Path}; returning markdown in API response only", filePath);
                filePath = null;
            }
        }

        return new ExportedMarkdownFile(contentType, relativePath.Replace('\\', '/'), filePath, markdown);
    }

    private string? TryResolveOutputRoot()
    {
        var configured = _exportOptions.OutputRootPath?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            _logger.LogDebug("ContentExport:OutputRootPath is not configured; skipping disk writes.");
            return null;
        }

        return Path.GetFullPath(configured);
    }
}
