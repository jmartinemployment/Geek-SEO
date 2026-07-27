namespace ContentWriterV3.Domain.Entities;

public class ClientProfileVersion : BaseEntity
{
    public Guid ProfileId { get; set; }
    public int Version { get; set; }
    public Dictionary<string, object> ApprovedFactsJson { get; set; } = new();
    public Dictionary<string, object> ProhibitedClaimsJson { get; set; } = new();
    public List<ClientBrandVoiceLink> BrandVoiceLinks { get; set; } = new();

    public ClientProfileVersion() { }

    public ClientProfileVersion(Guid profileId, int version)
    {
        ProfileId = profileId;
        Version = version;
    }
}
