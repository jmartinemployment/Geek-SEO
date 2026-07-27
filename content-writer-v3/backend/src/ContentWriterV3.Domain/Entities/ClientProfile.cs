namespace ContentWriterV3.Domain.Entities;

public class ClientProfile : BaseEntity
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ClientProfileVersion> Versions { get; set; } = new();

    public ClientProfile() { }

    public ClientProfile(Guid clientId, string name)
    {
        ClientId = clientId;
        Name = name;
    }

    public ClientProfileVersion CreateNewVersion(Dictionary<string, object> approvedFacts, Dictionary<string, object> prohibitedClaims)
    {
        var version = new ClientProfileVersion
        {
            ProfileId = Id,
            Version = (Versions.Count > 0 ? Versions.Max(v => v.Version) : 0) + 1,
            ApprovedFactsJson = approvedFacts,
            ProhibitedClaimsJson = prohibitedClaims
        };
        Versions.Add(version);
        return version;
    }
}
