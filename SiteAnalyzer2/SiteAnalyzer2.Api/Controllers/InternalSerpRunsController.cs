using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Api.Auth;
using SiteAnalyzer2.Api.Contracts;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Services.Pipeline;

namespace SiteAnalyzer2.Api.Controllers;

[ApiController]
[Route("internal/runs")]
[SerpWorkerAuth]
public class InternalSerpRunsController(
    IAnalysisRunRepository runRepository,
    SerpExternalCompletionService serpExternalCompletion) : ControllerBase
{
    [HttpGet("pending-serp")]
    public async Task<ActionResult<PendingSerpRunsResponse>> ListPending(CancellationToken ct)
    {
        var runs = await runRepository.ListPendingSerpRunsAsync(ct);
        return Ok(new PendingSerpRunsResponse(
            runs.Select(r => new PendingSerpRunItem(r.Id, r.ProjectId, r.Keyword, r.SerpProviderKey)).ToList()));
    }

    [HttpPost("{runId:guid}/stages/serp/claim")]
    public async Task<IActionResult> Claim(Guid runId, CancellationToken ct)
    {
        var status = await serpExternalCompletion.ClaimAsync(runId, ct);
        return status switch
        {
            SerpClaimStatus.Success => NoContent(),
            SerpClaimStatus.NotFound => NotFound(),
            SerpClaimStatus.AlreadyCompleted => Conflict(new { error = "SERP stage already completed." }),
            SerpClaimStatus.AlreadyClaimed => Conflict(new { error = "Run already claimed." }),
            SerpClaimStatus.Conflict => Conflict(new { error = "Claim lost to another worker." }),
            _ => BadRequest(new { error = "Run is not claimable." })
        };
    }

    [HttpPost("{runId:guid}/stages/serp/worker-result")]
    public async Task<IActionResult> WorkerResult(
        Guid runId,
        [FromBody] SerpWorkerResultRequest request,
        CancellationToken ct)
    {
        var input = new SerpWorkerResultInput(
            request.Success,
            request.Html,
            request.FailureMessage);

        var status = await serpExternalCompletion.CompleteAsync(runId, input, ct);
        return status switch
        {
            SerpWorkerResultStatus.Success => NoContent(),
            SerpWorkerResultStatus.NotFound => NotFound(),
            SerpWorkerResultStatus.AlreadyCompleted => Conflict(new { error = "SERP stage already completed." }),
            _ => BadRequest(new { error = "Run is not ready for worker result." })
        };
    }
}
