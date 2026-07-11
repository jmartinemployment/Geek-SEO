namespace ContentWriter.Application.Services.Figures;

public sealed class FigureRegenerationBlockedException : Exception
{
    public FigureRegenerationBlockedException(int readyCount, int publishedCount)
        : base(
            $"Regenerating figure briefs would affect {readyCount} Ready and {publishedCount} Published figures. " +
            "Pass confirmRegenerateWithArt=true to continue.")
    {
        ReadyCount = readyCount;
        PublishedCount = publishedCount;
    }

    public int ReadyCount { get; }
    public int PublishedCount { get; }
}
