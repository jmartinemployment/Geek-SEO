namespace GeekSeoBackend.Services;

/// <summary>
/// Pluggable Site Analyzer finding check (Semrush Site Audit–style pack).
/// Site-model pipeline runs first; checks emit typed findings afterward.
/// </summary>
public interface ISiteFindingCheck
{
    /// <summary>e.g. content_gap, broken_link, structured_data</summary>
    string FindingType { get; }

    Task<IReadOnlyList<SiteFindingDraft>> RunAsync(SiteFindingContext ctx, CancellationToken ct);
}

public sealed record SiteFindingContext(
    Guid ProfileId,
    Guid ProjectId,
    string Domain);

public sealed record SiteFindingDraft(
    string FindingType,
    string Severity,
    string Title,
    string Summary,
    string? AffectedUrl = null,
    string DetailsJson = "{}");

/// <summary>
/// Architecture probe: proves a second finding type can register without schema change.
/// Not enabled in production packs.
/// </summary>
public sealed class ArchitectureProbeFindingCheck : ISiteFindingCheck
{
    public string FindingType => "architecture_probe";

    public Task<IReadOnlyList<SiteFindingDraft>> RunAsync(SiteFindingContext ctx, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SiteFindingDraft>>([
            new SiteFindingDraft(
                FindingType,
                "info",
                "Architecture probe",
                "Second finding type registered without changing site-model stages.",
                AffectedUrl: null,
                DetailsJson: """{"probe":true}"""),
        ]);
}
