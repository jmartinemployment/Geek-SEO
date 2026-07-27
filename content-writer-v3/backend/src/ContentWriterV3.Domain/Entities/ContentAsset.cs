namespace ContentWriterV3.Domain.Entities;

public class ContentAsset : BaseEntity
{
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Draft;
    public List<ContentAssetVersion> Versions { get; set; } = new();

    public ContentAsset() { }

    public ContentAsset(Guid campaignId, string name, AssetType type)
    {
        CampaignId = campaignId;
        Name = name;
        Type = type;
    }
}

public enum AssetType
{
    Pillar,
    Companion
}

public enum AssetStatus
{
    Draft,
    ReadyForApproval,
    Approved,
    Published
}
