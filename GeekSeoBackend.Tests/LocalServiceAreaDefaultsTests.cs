using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Tests;

public sealed class LocalServiceAreaDefaultsTests
{
    [Theory]
    [InlineData(0, 5)]
    [InlineData(4, 5)]
    [InlineData(20, 20)]
    [InlineData(100, 100)]
    [InlineData(150, 100)]
    public void ClampRadiusMiles_stays_within_bounds(int input, int expected)
    {
        Assert.Equal(expected, LocalServiceAreaDefaults.ClampRadiusMiles(input));
    }

    [Fact]
    public void DefaultRadiusMiles_is_20()
    {
        Assert.Equal(20, LocalServiceAreaDefaults.DefaultRadiusMiles);
    }
}
