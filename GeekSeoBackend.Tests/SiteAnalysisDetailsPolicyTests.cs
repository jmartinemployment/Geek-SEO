using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalysisDetailsPolicyTests
{
    [Theory]
    [InlineData("queued", true)]
    [InlineData("processing", true)]
    [InlineData("complete", true)]
    [InlineData("failed", true)]
    [InlineData("pending", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStepLogAvailable_MatchesRunLifecycle(string? status, bool expected) =>
        Assert.Equal(expected, SiteAnalysisDetailsPolicy.IsStepLogAvailable(status));
}
