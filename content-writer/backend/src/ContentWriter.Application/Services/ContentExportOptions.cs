namespace ContentWriter.Application.Services;

public class ContentExportOptions
{
    public const string SectionName = "ContentExport";

    /// <summary>Root directory for markdown exports: {OutputRootPath}/{department}/{targetKeyword}/Pillar|Blog/</summary>
    public string OutputRootPath { get; set; } =
        "/Users/jeffmartin/Documents/Content-Writer-Output";
}
