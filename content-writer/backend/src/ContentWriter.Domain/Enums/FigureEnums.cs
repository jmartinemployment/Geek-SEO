namespace ContentWriter.Domain.Enums;

public enum FigureStatus
{
    Pending = 0,
    Ready = 1,
    Skipped = 2,
    Published = 3,
}

public static class FigureSourceType
{
    public const string Pillar = "pillar";
    public const string Blog = "blog";
    public const string ToolPrefix = "tool/";

    public static bool IsTool(string sourceType) =>
        sourceType.StartsWith(ToolPrefix, StringComparison.OrdinalIgnoreCase);

    public static string ForTool(string toolSlug) => $"{ToolPrefix}{toolSlug}";

    public static bool IsKnown(string sourceType) =>
        sourceType is Pillar or Blog || IsTool(sourceType);
}

public static class FigureSkipReason
{
    public const string SectionRemoved = "SectionRemoved";
    public const string UserSkipped = "UserSkipped";
}
