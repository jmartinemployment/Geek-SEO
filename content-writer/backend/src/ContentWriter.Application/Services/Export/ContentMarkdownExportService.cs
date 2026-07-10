using System.Text;
using ContentWriter.Application.DTOs;
using ContentWriter.Application.Providers;
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

public sealed record ExportedMarkdownFile(string ContentType, string FilePath);

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
            _companyProfile.BlogBaseUrl);

        var department = DepartmentNameResolver.Resolve(
            contentSet.ArticleUrl,
            contentSet.BlogUrl,
            project.ProjectUrl,
            project.Name,
            departmentOverride);

        var outputRoot = ResolveOutputRoot();
        var pillarDir = Path.Combine(outputRoot, department, "Pillar");
        var blogDir = Path.Combine(outputRoot, department, "Blog");
        Directory.CreateDirectory(pillarDir);
        Directory.CreateDirectory(blogDir);

        var exportedAt = DateTime.UtcNow;
        var written = new List<ExportedMarkdownFile>();

        if (contentSet.Article is not null
            && contentSet.Article.WordCount >= PillarBodyMinWords
            && !string.IsNullOrWhiteSpace(contentSet.ArticleSlug))
        {
            var pillarPath = Path.Combine(pillarDir, $"{contentSet.ArticleSlug}.md");
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
                ExportedAtUtc: exportedAt));

            await File.WriteAllTextAsync(pillarPath, markdown, Encoding.UTF8, cancellationToken);
            written.Add(new ExportedMarkdownFile("pillar", pillarPath));
            _logger.LogInformation("Exported pillar markdown to {Path}", pillarPath);
        }

        if (contentSet.Blog is not null
            && contentSet.Blog.WordCount > 0
            && !string.IsNullOrWhiteSpace(contentSet.BlogSlug))
        {
            var blogPath = Path.Combine(blogDir, $"{contentSet.BlogSlug}.md");
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
                ExportedAtUtc: exportedAt));

            await File.WriteAllTextAsync(blogPath, markdown, Encoding.UTF8, cancellationToken);
            written.Add(new ExportedMarkdownFile("blog", blogPath));
            _logger.LogInformation("Exported blog markdown to {Path}", blogPath);
        }

        if (written.Count == 0)
        {
            throw new ContentGenerationException(
                "Nothing to export. Complete the pillar body (Step 2) and/or blog (Step 3) before exporting.");
        }

        return new ExportMarkdownResult(department, written);
    }

    private string ResolveOutputRoot()
    {
        var configured = _exportOptions.OutputRootPath?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
            throw new ContentGenerationException("ContentExport:OutputRootPath is not configured.");

        return Path.GetFullPath(configured);
    }
}
