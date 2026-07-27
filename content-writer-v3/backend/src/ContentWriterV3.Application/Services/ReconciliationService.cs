using ContentWriterV3.Domain.Entities;
using System.Text.Json;

namespace ContentWriterV3.Application.Services;

public interface IReconciliationService
{
    Task<List<ReconciliationProposal>> GenerateProposalsAsync(
        Guid clientId,
        List<ResearchEvidence> newEvidence,
        List<PainPoint> existingPainPoints);
}

public class ReconciliationService : IReconciliationService
{
    public async Task<List<ReconciliationProposal>> GenerateProposalsAsync(
        Guid clientId,
        List<ResearchEvidence> newEvidence,
        List<PainPoint> existingPainPoints)
    {
        var proposals = new List<ReconciliationProposal>();

        // Analyze new evidence against existing pain points
        foreach (var evidence in newEvidence)
        {
            // Try to link to existing pain points
            var matchingPainPoints = FindMatchingPainPoints(evidence, existingPainPoints);

            if (matchingPainPoints.Count == 0)
            {
                // No match found - could suggest new pain point
                // For now, just log it
            }
            else
            {
                foreach (var painPoint in matchingPainPoints)
                {
                    // Generate proposal to link evidence to pain point
                    var proposal = new ReconciliationProposal(
                        Guid.Empty, // ResearchRunId set by caller
                        ProposalType.NewEvidenceLink,
                        JsonSerializer.Serialize(new
                        {
                            painPointId = painPoint.Id,
                            evidenceId = evidence.Id,
                            statement = evidence.Statement,
                            supportLevel = evidence.SupportLevel.ToString()
                        }));

                    proposal.PainPointId = painPoint.Id;
                    proposals.Add(proposal);
                }
            }
        }

        return await Task.FromResult(proposals);
    }

    private static List<PainPoint> FindMatchingPainPoints(
        ResearchEvidence evidence,
        List<PainPoint> painPoints)
    {
        // Simplified matching logic - in production, this would use semantic similarity
        var matches = new List<PainPoint>();

        foreach (var painPoint in painPoints)
        {
            // Check if evidence statement relates to pain point keywords
            var statementLower = evidence.Statement.ToLower();
            var nameMatch = painPoint.Name.ToLower();
            var descriptionMatch = painPoint.Description.ToLower();

            if (statementLower.Contains(nameMatch) || statementLower.Contains(descriptionMatch))
            {
                matches.Add(painPoint);
            }
        }

        return matches;
    }
}
