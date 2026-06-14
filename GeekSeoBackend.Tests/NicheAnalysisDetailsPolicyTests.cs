using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class NicheAnalysisDetailsPolicyTests
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
        Assert.Equal(expected, NicheAnalysisDetailsPolicy.IsStepLogAvailable(status));
}
