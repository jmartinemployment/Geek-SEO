using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SerpTermMatrixBuilderTests
{
    [Fact]
    public void Build_extracts_shared_terms_across_organic_rows()
    {
        var organic = new List<DeepSerpOrganic>
        {
            new() { Position = 1, Url = "https://a.com", Title = "Best CRM Software", Snippet = "Compare CRM software tools" },
            new() { Position = 2, Url = "https://b.com", Title = "Top CRM Tools", Snippet = "CRM software for teams" },
        };

        var matrix = SerpTermMatrixBuilder.Build(organic);

        Assert.NotEmpty(matrix.Terms);
        Assert.Contains("crm", matrix.Terms, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(2, matrix.Rows.Count);
        Assert.Equal(matrix.Terms.Count, matrix.Rows[0].Counts.Count);
    }
}
