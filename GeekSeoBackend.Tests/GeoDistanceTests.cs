using GeekSeoBackend.Services.LocalServiceArea;

namespace GeekSeoBackend.Tests;

public sealed class GeoDistanceTests
{
    [Fact]
    public void HaversineMiles_DelrayToBoca_IsWithinTwentyMiles()
    {
        // Delray Beach vs Boca Raton (~12 mi)
        var miles = GeoDistance.HaversineMiles(26.4615, -80.0728, 26.3683, -80.1289);
        Assert.InRange(miles, 6, 16);
        Assert.True(GeoDistance.IsWithinRadiusMiles(26.4615, -80.0728, 26.3683, -80.1289, 20));
    }

    [Fact]
    public void HaversineMiles_DelrayToMiami_IsOutsideTwentyMiles()
    {
        var miles = GeoDistance.HaversineMiles(26.4615, -80.0728, 25.7617, -80.1918);
        Assert.True(miles > 40);
        Assert.False(GeoDistance.IsWithinRadiusMiles(26.4615, -80.0728, 25.7617, -80.1918, 20));
    }
}
