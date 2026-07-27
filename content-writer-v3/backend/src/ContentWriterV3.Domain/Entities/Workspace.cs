namespace ContentWriterV3.Domain.Entities;

public class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public List<Client> Clients { get; set; } = new();
    public List<ContentCampaign> Campaigns { get; set; } = new();

    public Workspace() { }

    public Workspace(string name)
    {
        Name = name;
    }
}
