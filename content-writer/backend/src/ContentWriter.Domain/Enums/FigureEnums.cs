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
}

public static class FigureSkipReason
{
    public const string SectionRemoved = "SectionRemoved";
    public const string UserSkipped = "UserSkipped";
}
