using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;

namespace GeekSeo.Application.Services.Seo;

public sealed class WritingResearchContextLoader
{
    public Task<Result<WritingResearchContext>> LoadAsync(
        Guid userId,
        SeoContentDocument document,
        CancellationToken ct = default) =>
        Task.FromResult(Load(document, userId));

    public static Result<WritingResearchContext> Load(SeoContentDocument document, Guid userId)
    {
        if (!ResearchBackedWriteGate.IsResearchBacked(document))
            return Result<WritingResearchContext>.Failure(ContentWritingBlockMessage.Default);

        var export = ContentWriterKeywordBundleSerializer.TryDeserialize(document.KeywordBundleJson);
        if (export is null)
            return Result<WritingResearchContext>.Failure(
                "Frozen keyword bundle is missing. Re-open Content Writing from Site Analyzer with site_profile.");

        var gate = ResearchBackedWriteGate.ValidateAnalysisRunExport(export);
        if (!gate.IsSuccess)
            return Result<WritingResearchContext>.Failure(gate.Error ?? "Frozen keyword bundle is not valid for writing.");

        var location = string.IsNullOrWhiteSpace(document.TargetLocation)
            ? "United States"
            : document.TargetLocation;

        var context = ContentWriterSerpExportMapper.ToWritingResearchContext(
            export,
            userId,
            location,
            document.TargetKeyword);

        return Result<WritingResearchContext>.Success(ApplySiteFocus(context, document));
    }

    public static WritingResearchContext ApplySiteFocus(
        WritingResearchContext context,
        SeoContentDocument document)
    {
        var focus = SiteWritingFocusSerializer.TryDeserialize(document.SiteFocusJson);
        if (focus is null)
            return context;

        return context with
        {
            SiteFocus = focus,
            BusinessContext = SiteWritingFocusSerializer.ToBusinessContext(focus),
        };
    }
}
