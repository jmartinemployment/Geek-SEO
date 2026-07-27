namespace ContentWriterV3.Domain.Entities;

public class ContentAssetVersion : BaseEntity
{
    public Guid AssetId { get; set; }
    public int VersionNumber { get; set; }
    public string BodyDocumentJson { get; set; } = string.Empty;

    public ContentAssetVersion() { }

    public ContentAssetVersion(Guid assetId, int versionNumber, string bodyDocumentJson)
    {
        AssetId = assetId;
        VersionNumber = versionNumber;
        BodyDocumentJson = bodyDocumentJson;
    }
}
