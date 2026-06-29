namespace SiteAnalyzer2.Services.BusinessFocus;

public class StubBusinessFocusClassifier : IBusinessFocusClassifier
{
    public Task<BusinessFocusClassificationResult> ClassifyAsync(BusinessFocusInput input, CancellationToken ct = default)
    {
        var schema = """
            {
              "@context": "https://schema.org",
              "@type": "ProfessionalService",
              "name": "Example Business",
              "description": "Technology consulting services."
            }
            """;

        return Task.FromResult(new BusinessFocusClassificationResult(
            BusinessType: "ProfessionalService",
            PrimaryServices: ["Technology consulting", "Managed IT services"],
            ServiceArea: "United States",
            Description: "Provides technology consulting and managed IT services for small businesses.",
            GeneratedSchemaJson: schema,
            HasExistingSchema: input.HasExistingSchema,
            ExistingSchemaMatches: input.HasExistingSchema ? true : null));
    }
}
