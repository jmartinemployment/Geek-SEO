namespace GeekSeo.Application.Models.Seo;

public sealed class UpsertSubscriptionRequest
{
    public required string Tier { get; init; }
    public required string Status { get; init; }
    public string? PaypalSubscriptionId { get; init; }
    public DateTimeOffset? CurrentPeriodEnd { get; init; }
}
