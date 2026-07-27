namespace ContentWriterV3.Domain.Entities;

public class PainPoint : BaseEntity
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReaderSymptom { get; set; } = string.Empty;
    public string CostOfInaction { get; set; } = string.Empty;
    public string OfferTerminology { get; set; } = string.Empty;
    public List<string> Objections { get; set; } = new();
    public int Confidence { get; set; } = 50;
    public DateTime? StaleSince { get; set; }
    public List<PainPointEvidenceLink> EvidenceLinks { get; set; } = new();

    public PainPoint() { }

    public PainPoint(Guid clientId, string name, string description)
    {
        ClientId = clientId;
        Name = name;
        Description = description;
    }
}

public class PainPointEvidenceLink : BaseEntity
{
    public Guid PainPointId { get; set; }
    public Guid ResearchEvidenceId { get; set; }
    public EvidenceDimension Dimension { get; set; }

    public PainPointEvidenceLink() { }

    public PainPointEvidenceLink(Guid painPointId, Guid researchEvidenceId, EvidenceDimension dimension)
    {
        PainPointId = painPointId;
        ResearchEvidenceId = researchEvidenceId;
        Dimension = dimension;
    }
}

public enum EvidenceDimension
{
    Reader,
    Symptom,
    Cost,
    Offer,
    Objection
}
