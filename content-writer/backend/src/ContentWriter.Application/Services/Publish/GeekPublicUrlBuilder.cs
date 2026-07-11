using ContentWriter.Application.Services.Export;
using ContentWriter.Domain.Entities;

namespace ContentWriter.Application.Services.Publish;

public static class GeekPublicUrlBuilder
{
    public static string ResolveDepartment(Project project, string? departmentOverride = null) =>
        DepartmentNameResolver.Resolve(
            null,
            null,
            project.ProjectUrl,
            project.Name,
            departmentOverride);

    public static string ArticleUrl(string articleBaseUrl, string department, string slug) =>
        $"{articleBaseUrl.TrimEnd('/')}/{department}/{slug}";

    public static string BlogUrl(string blogBaseUrl, string department, string slug) =>
        $"{blogBaseUrl.TrimEnd('/')}/{department}/{slug}";

    public static string ApiSlugForArticle(string department, string slug) => $"use-cases/{department}/{slug}";

    public static string ApiSlugForBlog(string department, string slug) => $"blog/{department}/{slug}";

    public static string ArticlePath(string department, string slug) => $"/use-cases/{department}/{slug}";

    public static string BlogPath(string department, string slug) => $"/blog/{department}/{slug}";
}
