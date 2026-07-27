namespace ContentWriterV3.Domain.Entities;

public class ContentCampaign : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid ProfileVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public List<ContentAsset> Assets { get; set; } = new();
    public List<Job> Jobs { get; set; } = new();

    public ContentCampaign() { }

    public ContentCampaign(Guid clientId, Guid profileVersionId, string name, string keyword)
    {
        ClientId = clientId;
        ProfileVersionId = profileVersionId;
        Name = name;
        Keyword = keyword;
    }
}

public enum CampaignStatus
{
    Draft,
    Research,
    Strategy,
    Drafting,
    Published,
    Archived
}
