using ContentWriter.Application.DTOs;
using ContentWriter.Domain.Entities;

namespace ContentWriter.Application.Services.Publish;

public static class GeneratedContentPresentation
{
    public const string HomeList = "home_list";
    public const string DepartmentList = "department_list";
    public const string Hero = "hero";
    public const string NewspaperWire = "newspaper_wire";
    public const string PillarPageContent = "pillar_page_content";
    public const string ToolPageContent = "tool_page_content";
    public const string Advertisement = "advertisement";

    public static string PublishTitle(GeneratedContent row) =>
        string.IsNullOrWhiteSpace(row.DisplayTitle) ? row.Title : row.DisplayTitle.Trim();

    public static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void ApplyPillarFields(GeneratedContent row, ArticleMetadataDraft metadata)
    {
        row.HomeUseCaseExcerpt = metadata.HomeUseCaseExcerpt.Trim();
        row.DepartmentListExcerpt = metadata.DepartmentListExcerpt.Trim();
        row.HeroExcerpt = metadata.HeroExcerpt.Trim();
        row.NewspaperExcerpt = metadata.NewspaperExcerpt.Trim();
        row.PillarPageUseCaseExcerpt = metadata.PillarPageUseCaseExcerpt.Trim();
    }

    public static void ApplyBlogFields(GeneratedContent row, BlogMetadataDraft metadata)
    {
        row.DepartmentListExcerpt = metadata.DepartmentListExcerpt.Trim();
        row.HeroExcerpt = metadata.HeroExcerpt.Trim();
        row.NewspaperExcerpt = metadata.NewspaperExcerpt.Trim();
        row.Advertisement = TrimOrNull(metadata.Advertisement);
    }

    public static void ApplyToolFields(GeneratedContent row, ToolMetadataDraft metadata)
    {
        row.DepartmentListExcerpt = metadata.DepartmentListExcerpt.Trim();
        row.HeroExcerpt = metadata.HeroExcerpt.Trim();
        row.NewspaperExcerpt = metadata.NewspaperExcerpt.Trim();
        row.ToolPageExcerpt = metadata.ToolPageExcerpt.Trim();
        row.Advertisement = TrimOrNull(metadata.Advertisement);
    }

    public static Dictionary<string, string> BuildPresentationMap(GeneratedContent row, string contentRole)
    {
        var map = new Dictionary<string, string>();

        void Add(string surface, string? copy)
        {
            var trimmed = TrimOrNull(copy);
            if (trimmed is not null)
                map[surface] = trimmed;
        }

        switch (contentRole)
        {
            case "pillar":
                Add(HomeList, row.HomeUseCaseExcerpt);
                Add(DepartmentList, row.DepartmentListExcerpt);
                Add(Hero, row.HeroExcerpt);
                Add(NewspaperWire, row.NewspaperExcerpt);
                Add(PillarPageContent, row.PillarPageUseCaseExcerpt);
                break;
            case "blog":
                Add(DepartmentList, row.DepartmentListExcerpt);
                Add(Hero, row.HeroExcerpt);
                Add(NewspaperWire, row.NewspaperExcerpt);
                Add(Advertisement, row.Advertisement);
                break;
            case "tool":
                Add(DepartmentList, row.DepartmentListExcerpt);
                Add(Hero, row.HeroExcerpt);
                Add(NewspaperWire, row.NewspaperExcerpt);
                Add(ToolPageContent, row.ToolPageExcerpt);
                Add(Advertisement, row.Advertisement);
                break;
        }

        return map;
    }
}
