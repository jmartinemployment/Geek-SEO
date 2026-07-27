namespace ContentWriterV3.Api.Dtos;

public class CreateResearchRunRequest
{
    public Guid CampaignId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public decimal MaxBudget { get; set; } = 5.0m;
}

public class ResearchRunResponse
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DiscoveredSourceCount { get; set; }
    public decimal SpentBudget { get; set; }
    public decimal MaxBudget { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PainPointResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReaderSymptom { get; set; } = string.Empty;
    public string CostOfInaction { get; set; } = string.Empty;
    public string OfferTerminology { get; set; } = string.Empty;
    public List<string> Objections { get; set; } = new();
    public int Confidence { get; set; }
    public DateTime? StaleSince { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReconciliationProposalResponse
{
    public Guid Id { get; set; }
    public string ProposalType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public object ProposedData { get; set; } = new { };
    public DateTime CreatedAt { get; set; }
}
