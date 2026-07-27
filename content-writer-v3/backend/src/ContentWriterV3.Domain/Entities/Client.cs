namespace ContentWriterV3.Domain.Entities;

public class Client : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ClientProfile> Profiles { get; set; } = new();
    public List<ContentCampaign> Campaigns { get; set; } = new();
    public List<PainPoint> PainPoints { get; set; } = new();

    public Client() { }

    public Client(Guid workspaceId, string name)
    {
        WorkspaceId = workspaceId;
        Name = name;
    }
}
