using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ContentWriterV3.Infrastructure.Jobs.Handlers;

public class InitiateResearchHandler : JobHandler<InitiateResearchPayload>
{
    private readonly ContentWriterV3DbContext _dbContext;
    private readonly ILogger<InitiateResearchHandler> _logger;

    public override string JobType => "InitiateResearch";

    public InitiateResearchHandler(ContentWriterV3DbContext dbContext, ILogger<InitiateResearchHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(Job job, InitiateResearchPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting research run for campaign {CampaignId} with keyword {Keyword}",
            payload.CampaignId, payload.Keyword);

        var researchRun = new ResearchRun(payload.CampaignId, payload.Keyword, payload.MaxBudget);
        researchRun.MarkRunning();

        _dbContext.ResearchRuns.Add(researchRun);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Research run {ResearchRunId} initiated", researchRun.Id);
    }
}

public class InitiateResearchPayload
{
    public Guid CampaignId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public decimal MaxBudget { get; set; } = 5.0m;
}
