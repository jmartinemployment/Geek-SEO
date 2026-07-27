namespace ContentWriterV3.Api.Dtos;

public class CreateCampaignRequest
{
    public Guid ClientId { get; set; }
    public Guid ProfileVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
}

public class CampaignResponse
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
