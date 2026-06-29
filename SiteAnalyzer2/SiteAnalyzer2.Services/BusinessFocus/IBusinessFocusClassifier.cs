namespace SiteAnalyzer2.Services.BusinessFocus;

public interface IBusinessFocusClassifier
{
    Task<BusinessFocusClassificationResult> ClassifyAsync(BusinessFocusInput input, CancellationToken ct = default);
}
