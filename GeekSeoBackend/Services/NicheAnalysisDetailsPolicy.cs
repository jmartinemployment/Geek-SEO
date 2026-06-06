namespace GeekSeoBackend.Services;

/// <summary>When <c>GET analysis-details</c> may return step log data (including empty while queued).</summary>
internal static class NicheAnalysisDetailsPolicy
{
    internal static bool IsStepLogAvailable(string? status) =>
        status is "complete" or "failed" or "processing" or "queued";
}
