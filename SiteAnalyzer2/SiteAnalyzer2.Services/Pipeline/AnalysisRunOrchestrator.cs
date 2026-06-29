using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Services.BusinessFocus;
using SiteAnalyzer2.Services.Filtering;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Serp;

namespace SiteAnalyzer2.Services.Pipeline;

public class AnalysisRunOrchestrator(
    IAnalysisRunRepository runRepository,
    SerpDiscoveryService serpDiscoveryService,
    RelevanceFilterService relevanceFilterService,
    PageFetchService pageFetchService,
    PageExtractionService pageExtractionService,
    BusinessFocusClassificationService businessFocusClassificationService,
    LinkGraphBuilderService linkGraphBuilderService,
    BoundedPageRankService boundedPageRankService,
    ComparisonService comparisonService,
    RunGateService runGateService)
{
    private static readonly PipelineStage[] StageOrder =
    [
        PipelineStage.Serp,
        PipelineStage.Filter,
        PipelineStage.Fetch,
        PipelineStage.Extract,
        PipelineStage.Graph,
        PipelineStage.PageRank,
        PipelineStage.Comparison
    ];

    public async Task<AnalysisRun> StartRunAsync(AnalysisRun run, CancellationToken ct = default)
    {
        run.Status = RunStatus.Running;
        run.CurrentStage = PipelineStage.Serp;
        await runRepository.SaveAsync(run, ct);

        if (SerpExecutionConfiguration.IsExternal || SerpExecutionConfiguration.IsManual)
            return (await runRepository.GetByIdAsync(run.Id, ct))!;

        return await ExecuteSerpStageAsync(run, ct);
    }

    private async Task<AnalysisRun> ExecuteSerpStageAsync(AnalysisRun run, CancellationToken ct)
    {
        try
        {
            var fixturePath = SerpFixtureLocator.ResolveDefaultHtmlPath();
            var html = await File.ReadAllTextAsync(fixturePath, ct);
            await serpDiscoveryService.ImportFixtureHtmlAsync(run, html, ct);
        }
        catch (Exception ex)
        {
            await runGateService.FailStageAsync(run, PipelineStage.Serp, ex.Message, ct);
        }

        return (await runRepository.GetByIdAsync(run.Id, ct))!;
    }

    public async Task<AnalysisRun> AdvanceStageAsync(Guid runId, PipelineStage stage, CancellationToken ct = default)
    {
        var run = await runRepository.GetByIdAsync(runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        ValidateAdvance(run, stage);

        run.Status = RunStatus.Running;
        run.CurrentStage = stage;
        await runRepository.SaveAsync(run, ct);

        try
        {
            switch (stage)
            {
                case PipelineStage.Filter:
                    await relevanceFilterService.RunFilterStageAsync(runId, ct);
                    break;
                case PipelineStage.Fetch:
                    await pageFetchService.RunFetchStageAsync(runId, ct);
                    break;
                case PipelineStage.Extract:
                    await pageExtractionService.RunExtractStageAsync(runId, ct);
                    await businessFocusClassificationService.RunAfterExtractAsync(runId, ct);
                    break;
                case PipelineStage.Graph:
                    await linkGraphBuilderService.RunGraphStageAsync(runId, ct);
                    break;
                case PipelineStage.PageRank:
                    await boundedPageRankService.RunPageRankStageAsync(runId, ct);
                    break;
                case PipelineStage.Comparison:
                    await comparisonService.RunComparisonStageAsync(runId, ct);
                    break;
                default:
                    throw new InvalidOperationException($"Stage {stage} cannot be advanced via API.");
            }

            await runGateService.EvaluateAndPersistAsync(run, stage, serpSupplement: null, ct);
        }
        catch (Exception)
        {
            run.Status = RunStatus.Failed;
            await runRepository.SaveAsync(run, ct);
            throw;
        }

        return (await runRepository.GetByIdAsync(runId, ct))!;
    }

    /// <summary>
    /// Repairs runs left in Running after a completed gate (e.g. double-click or client timeout).
    /// </summary>
    public async Task<AnalysisRun?> ReconcileStuckRunAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await runRepository.GetByIdAsync(runId, ct);
        if (run is null || run.Status != RunStatus.Running)
            return run;

        var latestGate = run.RunGates.OrderByDescending(g => g.CheckedAt).FirstOrDefault();
        if (latestGate is not { Passed: true, Stage: var gateStage })
            return run;

        if (gateStage != run.CurrentStage)
            return run;

        run.Status = RunStatus.SerpReady;
        await runRepository.SaveAsync(run, ct);
        return run;
    }

    private static void ValidateAdvance(AnalysisRun run, PipelineStage requestedStage)
    {
        if (run.Status != RunStatus.SerpReady)
            throw new InvalidOperationException($"Run {run.Id} is not ready to advance (status={run.Status}).");

        if (run.Status == RunStatus.Failed)
            throw new InvalidOperationException($"Run {run.Id} has failed and cannot be advanced.");

        var expectedStage = GetNextStage(run.CurrentStage);
        if (expectedStage != requestedStage)
        {
            throw new InvalidOperationException(
                $"Cannot advance to {requestedStage}; expected next stage is {expectedStage}.");
        }

        var priorGate = run.RunGates.FirstOrDefault(g => g.Stage == run.CurrentStage);
        if (priorGate is { Passed: false })
            throw new InvalidOperationException($"Prior stage {run.CurrentStage} did not pass its gate.");
    }

    private static PipelineStage GetNextStage(PipelineStage? currentStage)
    {
        if (currentStage == null)
            return PipelineStage.Serp;

        var index = Array.IndexOf(StageOrder, currentStage.Value);
        if (index < 0 || index >= StageOrder.Length - 1)
            throw new InvalidOperationException($"No stage follows {currentStage}.");

        return StageOrder[index + 1];
    }
}
