namespace ContentWriterV3.Domain.Entities;

public class ClientBrandVoiceLink : BaseEntity
{
    public Guid ProfileVersionId { get; set; }
    public Guid BrandVoiceId { get; set; }

    public ClientBrandVoiceLink() { }

    public ClientBrandVoiceLink(Guid profileVersionId, Guid brandVoiceId)
    {
        ProfileVersionId = profileVersionId;
        BrandVoiceId = brandVoiceId;
    }
}
