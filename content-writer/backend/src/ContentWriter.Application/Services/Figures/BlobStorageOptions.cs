namespace ContentWriter.Application.Services.Figures;

public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string ReadWriteToken { get; set; } = string.Empty;
}
