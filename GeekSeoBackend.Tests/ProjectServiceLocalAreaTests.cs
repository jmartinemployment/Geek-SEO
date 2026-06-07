using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ProjectServiceLocalAreaTests
{
    [Fact]
    public void NormalizeCreate_clamps_radius_and_trims_address()
    {
        var normalized = ProjectService.NormalizeCreate(new CreateProjectRequest
        {
            Name = "Test",
            Url = "https://example.com",
            BusinessAddress = "  123 Main St  ",
            ServiceRadiusMiles = 150,
        });

        Assert.Equal("123 Main St", normalized.BusinessAddress);
        Assert.Equal(100, normalized.ServiceRadiusMiles);
    }

    [Fact]
    public void NormalizeUpdate_blank_address_becomes_null()
    {
        var normalized = ProjectService.NormalizeUpdate(new UpdateProjectRequest
        {
            BusinessAddress = "   ",
            ServiceRadiusMiles = 3,
        });

        Assert.Null(normalized.BusinessAddress);
        Assert.Equal(5, normalized.ServiceRadiusMiles);
    }
}
