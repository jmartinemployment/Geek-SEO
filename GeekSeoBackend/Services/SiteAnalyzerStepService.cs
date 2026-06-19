using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Services.NicheExtraction;
using GeekSeoBackend.Services.SiteAnalyzer;
using GeekSeoBackend.Infrastructure;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services;

public sealed class SiteAnalyzerStepService(
    ISiteResearchRepository siteResearch,
    IUrlResearchRepository urlResearch,
    IProjectRepository projects,
    SitemapExtractor sitemap,
    SitePageCrawler pageCrawler,
    ISerpResearchPackService packService,
    PlaywrightBrowserHolder? playwrightHolder,
    ILogger<SiteAnalyzerStepService> logger)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<SiteAnalyzerProjectStateResponse>> GetStateAsync(
        Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            return Result<SiteAnalyzerProjectStateResponse>.Failure("Access denied");

        var siteUrl = project.Value.Url.TrimEnd('/');
        var siteRow = await siteResearch.GetOrCreateForProjectAsync(
            userId,
            new CreateSiteResearchRequest { ProjectId = projectId, SiteUrl = siteUrl },
            ct);
        if (!siteRow.IsSuccess || siteRow.Value is null)
            return Result<SiteAnalyzerProjectStateResponse>.Failure(siteRow.Error ?? "Could not load site research");

        var siteSteps = await siteResearch.GetStepRunsForSiteAsync(siteRow.Value.Id, ct);
        var siteWithPages = await siteResearch.GetWithPagesAsync(siteRow.Value.Id, ct);
        var siteIndexSteps = BuildStepResponses(
            1,
            4,
            siteSteps.Value ?? [],
            siteWithPages.IsSuccess && siteWithPages.Value is not null
                ? step => SiteAnalyzerStepValidators.ValidateSiteIndexStep(step, siteWithPages.Value)
                : null);
        var siteIndexComplete = siteIndexSteps.All(s =>
            string.Equals(s.Status, "green", StringComparison.OrdinalIgnoreCase));
        var firstRedSiteIndexStep = siteIndexSteps
            .FirstOrDefault(s => string.Equals(s.Status, "red", StringComparison.OrdinalIgnoreCase))
            ?.StepNumber;

        var packSummaries = await urlResearch.ListSummaryByProjectAsync(projectId, ct);
        var packList = new List<SiteAnalyzerPackSummaryResponse>();

        foreach (var summary in (packSummaries.Value ?? [])
                     .Where(p => p.Status is not "failed")
                     .OrderByDescending(p => p.CreatedAt))
        {
            var full = await urlResearch.GetFullAsync(summary.Id, ct);
            var packStepRuns = await siteResearch.GetStepRunsForPackAsync(summary.Id, ct);
            var steps = BuildStepResponses(
                5,
                10,
                packStepRuns.Value ?? [],
                full.IsSuccess && full.Value is not null
                    ? step => step is >= 5 and <= 9
                        ? SiteAnalyzerStepValidators.ValidatePackStep(step, full.Value)
                        : SiteAnalyzerGateResult.Pass()
                    : null);
            var handoffReady = full.IsSuccess
                && full.Value is not null
                && SiteAnalyzerPackValidator.IsHandoffReady(full.Value);

            packList.Add(new SiteAnalyzerPackSummaryResponse
            {
                UrlResearchId = summary.Id,
                Keyword = summary.DerivedKeyword,
                Location = full.Value?.SearchLocation ?? "United States",
                DataQuality = summary.DataQuality,
                Status = summary.Status,
                Steps = steps,
                FirstRedStep = steps
                    .FirstOrDefault(s => string.Equals(s.Status, "red", StringComparison.OrdinalIgnoreCase))
                    ?.StepNumber,
                HandoffReady = handoffReady,
                ResearchedAt = summary.ResearchedAt,
                CreatedAt = summary.CreatedAt,
            });
        }

        return Result<SiteAnalyzerProjectStateResponse>.Success(new SiteAnalyzerProjectStateResponse
        {
            ProjectId = projectId,
            SiteResearchId = siteRow.Value.Id,
            SiteUrl = siteUrl,
            SiteIndexSteps = siteIndexSteps,
            SiteIndexComplete = siteIndexComplete,
            FirstRedSiteIndexStep = firstRedSiteIndexStep,
            Packs = packList,
        });
    }

    public async Task<Result<SiteAnalyzerStepResponse>> RunSiteIndexStepAsync(
        Guid userId, Guid projectId, int step, CancellationToken ct = default)
    {
        if (step is < 1 or > 4)
            return Result<SiteAnalyzerStepResponse>.Failure("Site index steps are 1–4.");

        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure("Access denied");

        var siteUrl = project.Value.Url.TrimEnd('/');
        var siteRow = await siteResearch.GetOrCreateForProjectAsync(
            userId,
            new CreateSiteResearchRequest { ProjectId = projectId, SiteUrl = siteUrl },
            ct);
        if (!siteRow.IsSuccess || siteRow.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure(siteRow.Error ?? "Could not load site research");

        var prior = await siteResearch.GetStepRunsForSiteAsync(siteRow.Value.Id, ct);
        if (!SiteAnalyzerStepProgression.PriorStepsGreen(step, prior.Value ?? []))
            return Result<SiteAnalyzerStepResponse>.Failure($"Step {step - 1} must be green before running step {step}.");

        var siteWithPages = await siteResearch.GetWithPagesAsync(siteRow.Value.Id, ct);
        if (!siteWithPages.IsSuccess || siteWithPages.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure(siteWithPages.Error ?? "Could not load site index");

        var blocked = await BlockIfPriorSiteStepsFailAsync(
            siteRow.Value.Id, null, step, siteWithPages.Value, ct);
        if (blocked is not null)
            return blocked;

        return step switch
        {
            1 => await RunStep1Async(userId, siteRow.Value, siteUrl, ct),
            2 => await RunStep2Async(userId, siteRow.Value, siteUrl, ct),
            3 => await RunStep3Async(userId, siteRow.Value, ct),
            4 => await RunStep4Async(userId, siteRow.Value, siteUrl, ct),
            _ => Result<SiteAnalyzerStepResponse>.Failure("Invalid step"),
        };
    }

    public async Task<Result<CreateSiteAnalyzerPackResponse>> CreatePackAsync(
        Guid userId, Guid projectId, CreateSiteAnalyzerPackRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return Result<CreateSiteAnalyzerPackResponse>.Failure("Keyword is required.");

        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            return Result<CreateSiteAnalyzerPackResponse>.Failure("Access denied");

        var siteUrl = project.Value.Url.TrimEnd('/');
        var siteRow = await siteResearch.GetOrCreateForProjectAsync(
            userId,
            new CreateSiteResearchRequest { ProjectId = projectId, SiteUrl = siteUrl },
            ct);
        if (!siteRow.IsSuccess || siteRow.Value is null)
            return Result<CreateSiteAnalyzerPackResponse>.Failure(siteRow.Error ?? "Could not load site research");

        var siteSteps = await siteResearch.GetStepRunsForSiteAsync(siteRow.Value.Id, ct);
        for (var s = 1; s <= 4; s++)
        {
            var row = (siteSteps.Value ?? []).FirstOrDefault(r => r.StepNumber == s);
            if (row is null || !string.Equals(row.Status, "green", StringComparison.OrdinalIgnoreCase))
                return Result<CreateSiteAnalyzerPackResponse>.Failure($"Site index step {s} must be green before starting a keyword pack.");
        }

        var location = string.IsNullOrWhiteSpace(request.Location) ? "United States" : request.Location.Trim();
        var keyword = request.Keyword.Trim();
        var queued = await urlResearch.CreateQueuedAsync(
            userId,
            new CreateUrlResearchQueuedRequest
            {
                ProjectId = projectId,
                SourceUrl = siteUrl,
                SiteResearchId = siteRow.Value.Id,
                DerivedKeyword = keyword,
                SearchLocation = location,
            },
            ct);
        if (!queued.IsSuccess || queued.Value is null)
            return Result<CreateSiteAnalyzerPackResponse>.Failure(queued.Error ?? "Could not create keyword pack");

        return Result<CreateSiteAnalyzerPackResponse>.Success(new CreateSiteAnalyzerPackResponse
        {
            UrlResearchId = queued.Value.Id,
            Keyword = keyword,
            Location = location,
        });
    }

    public async Task<Result<SiteAnalyzerStepResponse>> RunPackStepAsync(
        Guid userId, Guid urlResearchId, int step, CancellationToken ct = default)
    {
        if (step is < 5 or > 10)
            return Result<SiteAnalyzerStepResponse>.Failure("Keyword pack steps are 5–10.");

        var head = await urlResearch.GetHeadAsync(urlResearchId, ct);
        if (!head.IsSuccess || head.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure(head.Error ?? "Keyword pack not found");
        if (head.Value.UserId != userId)
            return Result<SiteAnalyzerStepResponse>.Failure("Access denied");

        if (head.Value.SiteResearchId is null)
            return Result<SiteAnalyzerStepResponse>.Failure("Site index not linked to this pack.");

        var site = await siteResearch.GetWithPagesAsync(head.Value.SiteResearchId.Value, ct);
        if (!site.IsSuccess || site.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure(site.Error ?? "Site index not found");

        var full = await urlResearch.GetFullAsync(urlResearchId, ct);
        if (!full.IsSuccess || full.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure(full.Error ?? "Keyword pack not found");

        await ReconcilePackStepRunsAsync(site.Value.Id, urlResearchId, step, full.Value, ct);

        var packSteps = await siteResearch.GetStepRunsForPackAsync(urlResearchId, ct);
        if (!SiteAnalyzerStepProgression.PriorStepsGreen(step, packSteps.Value ?? [], minStep: 5))
            return Result<SiteAnalyzerStepResponse>.Failure($"Step {step - 1} must be green before running step {step}.");

        var blocked = step < 10
            ? await BlockIfPriorPackStepsFailAsync(site.Value.Id, urlResearchId, step, full.Value, ct)
            : null;
        if (blocked is not null)
            return blocked;

        return step switch
        {
            5 => await RunStep5Async(userId, head.Value, site.Value, ct),
            6 => await RunStep6Async(userId, head.Value, site.Value, ct),
            7 => await RunStep7Async(userId, head.Value, site.Value, ct),
            8 => await RunStep8Async(userId, head.Value, site.Value, ct),
            9 => await RunStep9Async(userId, head.Value, site.Value, ct),
            10 => await RunStep10Async(userId, head.Value, site.Value, ct),
            _ => Result<SiteAnalyzerStepResponse>.Failure("Invalid step"),
        };
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep1Async(
        Guid userId, GeekSeo.Persistence.Entities.SeoSiteResearch site, string siteUrl, CancellationToken ct)
    {
        var log = new List<string>();
        await MarkRunningAsync(site.Id, null, 1, ct);

        var data = await sitemap.ExtractAsync(siteUrl, ct);
        var urls = data.SampleUrls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (urls.Count == 0 && data.TotalUrlsScanned > 0)
            urls.Add(siteUrl);
        log.Add($"Discovered {data.TotalUrlsScanned} URL(s) from sitemap ({urls.Count} stored for crawl).");

        var gate = SiteAnalyzerGates.Step1(urls.Count);
        if (!gate.Passed)
            return await FinishStepAsync(site.Id, null, 1, gate, log, new Dictionary<string, int> { ["urls"] = urls.Count }, ct);

        await siteResearch.PersistStep1Async(site.Id, new SiteResearchStep1Write { DiscoveredUrls = urls }, ct);
        return await FinishStepAsync(site.Id, null, 1, gate, log, new Dictionary<string, int> { ["urls"] = urls.Count }, ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep2Async(
        Guid userId, GeekSeo.Persistence.Entities.SeoSiteResearch site, string siteUrl, CancellationToken ct)
    {
        var log = new List<string>();
        await MarkRunningAsync(site.Id, null, 2, ct);

        var full = await siteResearch.GetWithPagesAsync(site.Id, ct);
        var discovered = ParseUrlList(full.Value?.DiscoveredUrlsJson);
        IBrowser? browser = playwrightHolder?.Browser;
        var crawl = await pageCrawler.CrawlAsync(siteUrl, discovered, browser, ct, SiteAnalyzerGates.MaxCrawlPages);
        log.Add($"Crawl finished: {crawl.PagesFetched}/{crawl.PagesAttempted} page(s).");

        var gate = SiteAnalyzerGates.Step2(crawl.PagesFetched);
        if (!gate.Passed)
            return await FinishStepAsync(site.Id, null, 2, gate, log, new Dictionary<string, int> { ["pages"] = crawl.PagesFetched }, ct);

        var pages = crawl.Pages.Select(p => new SiteResearchPageWrite
        {
            Url = p.Url,
            Html = p.Html,
        }).ToList();

        await siteResearch.ReplacePagesAsync(site.Id, pages, ct);
        return await FinishStepAsync(site.Id, null, 2, gate, log, new Dictionary<string, int> { ["pages"] = crawl.PagesFetched }, ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep3Async(
        Guid userId, GeekSeo.Persistence.Entities.SeoSiteResearch site, CancellationToken ct)
    {
        var log = new List<string>();
        await MarkRunningAsync(site.Id, null, 3, ct);

        var full = await siteResearch.GetWithPagesAsync(site.Id, ct);
        var crawled = full.Value?.Pages ?? [];
        var writes = new List<SiteResearchPageWrite>();
        var failed = 0;

        foreach (var page in crawled)
        {
            try
            {
                var headings = PageHtmlSignalExtractor.ExtractHeadings(page.Html);
                var jsonLd = PageHtmlSignalExtractor.ExtractJsonLdBlocks(page.Html);
                if (headings.Count == 0 && jsonLd.Count == 0)
                    throw new InvalidOperationException("No headings or JSON-LD found");

                writes.Add(new SiteResearchPageWrite
                {
                    Url = page.Url,
                    Html = page.Html,
                    HeadingsJson = PageHtmlSignalExtractor.SerializeHeadings(headings),
                    JsonLdJson = PageHtmlSignalExtractor.SerializeJsonLd(jsonLd),
                    ExtractSuccess = true,
                });
                log.Add($"OK {page.Url}: {headings.Count} heading(s), {jsonLd.Count} JSON-LD block(s).");
            }
            catch (Exception ex)
            {
                failed++;
                writes.Add(new SiteResearchPageWrite
                {
                    Url = page.Url,
                    Html = page.Html,
                    HeadingsJson = "[]",
                    JsonLdJson = "[]",
                    ExtractSuccess = false,
                    ExtractError = ex.Message,
                });
                log.Add($"FAIL {page.Url}: {ex.Message}");
            }
        }

        var extracted = writes.Count(w => w.ExtractSuccess);
        var gate = SiteAnalyzerGates.Step3(crawled.Count, extracted, failed);
        if (!gate.Passed)
        {
            return await FinishStepAsync(
                site.Id,
                null,
                3,
                gate,
                log,
                new Dictionary<string, int> { ["crawled"] = crawled.Count, ["failed"] = failed },
                ct);
        }

        await siteResearch.ReplacePagesAsync(site.Id, writes, ct);
        return await FinishStepAsync(
            site.Id,
            null,
            3,
            gate,
            log,
            new Dictionary<string, int> { ["crawled"] = crawled.Count, ["failed"] = failed },
            ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep4Async(
        Guid userId, GeekSeo.Persistence.Entities.SeoSiteResearch site, string siteUrl, CancellationToken ct)
    {
        var log = new List<string>();
        await MarkRunningAsync(site.Id, null, 4, ct);

        var full = await siteResearch.GetWithPagesAsync(site.Id, ct);
        var pages = full.Value?.Pages.Where(p => p.ExtractSuccess).ToList() ?? [];
        var origin = new Uri(siteUrl).GetLeftPart(UriPartial.Authority);

        var links = new List<object>();
        foreach (var page in pages)
        {
            foreach (var target in SitePageCrawler.ExtractSameOriginLinks(page.Html, page.Url, origin))
            {
                links.Add(new { from = page.Url, to = target });
            }
        }

        var summaryParts = pages
            .SelectMany(p => PageHtmlSignalExtractor.ExtractHeadings(p.Html).Where(h => h.Level <= 2).Select(h => h.Text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12);
        var summary = string.Join("; ", summaryParts);
        if (string.IsNullOrWhiteSpace(summary))
            summary = $"Site index for {siteUrl} ({pages.Count} page(s)).";

        var linkJson = JsonSerializer.Serialize(links, Json);
        log.Add($"Summary length {summary.Length}; {links.Count} internal link(s).");

        var gate = SiteAnalyzerGates.Step4(!string.IsNullOrWhiteSpace(summary), links.Count > 0);
        if (!gate.Passed)
            return await FinishStepAsync(site.Id, null, 4, gate, log, new Dictionary<string, int> { ["links"] = links.Count }, ct);

        await siteResearch.PersistStep4Async(site.Id, new SiteResearchStep4Write
        {
            BusinessSummary = summary,
            InternalLinkMapJson = linkJson,
        }, ct);

        return await FinishStepAsync(site.Id, null, 4, gate, log, new Dictionary<string, int> { ["links"] = links.Count }, ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep5Async(
        Guid userId,
        GeekSeo.Persistence.Entities.SeoUrlResearch pack,
        GeekSeo.Persistence.Entities.SeoSiteResearch site,
        CancellationToken ct)
    {
        var log = new List<string>();
        await ResetDownstreamPackStepRunsAsync(site.Id, pack.Id, fromStep: 6, ct);
        await MarkRunningAsync(site.Id, pack.Id, 5, ct);

        var built = await packService.BuildAsync(userId, new UrlAnalyzerResearchRequest
        {
            Keyword = pack.DerivedKeyword,
            Url = pack.SourceUrl,
            Location = pack.SearchLocation,
            BusinessContext = site.BusinessSummary,
        }, ct);

        if (!built.IsSuccess || built.Value is null)
        {
            var fail = SiteAnalyzerGateResult.Fail(built.Error ?? "SERP fetch failed");
            return await FinishStepAsync(site.Id, pack.Id, 5, fail, [built.Error ?? "SERP fetch failed"], null, ct);
        }

        var p = built.Value;
        log.Add($"Organic={p.Organic.Count}, PAA={p.Paa.Count}, PASF={p.Pasf.Count}, PAF={p.Paf.Type}.");

        var gate = SiteAnalyzerStepValidators.ValidatePackStepFromBuild(5, p);
        if (!gate.Passed)
        {
            return await FinishStepAsync(
                site.Id,
                pack.Id,
                5,
                gate,
                log,
                new Dictionary<string, int> { ["organic"] = p.Organic.Count, ["paa"] = p.Paa.Count, ["pasf"] = p.Pasf.Count },
                ct);
        }

        var write = UrlResearchPackMapper.ToStep5SerpWrite(p, "running");
        await urlResearch.PersistFullAsync(pack.Id, write, ct);

        return await FinishStepAsync(
            site.Id,
            pack.Id,
            5,
            gate,
            log,
            new Dictionary<string, int> { ["organic"] = p.Organic.Count, ["paa"] = p.Paa.Count, ["pasf"] = p.Pasf.Count },
            ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep6Async(
        Guid userId,
        GeekSeo.Persistence.Entities.SeoUrlResearch pack,
        GeekSeo.Persistence.Entities.SeoSiteResearch site,
        CancellationToken ct)
    {
        var log = new List<string> { "Crawling competitor pages from SERP organic results." };
        await ResetDownstreamPackStepRunsAsync(site.Id, pack.Id, fromStep: 7, ct);
        await MarkRunningAsync(site.Id, pack.Id, 6, ct);

        var full = await urlResearch.GetFullAsync(pack.Id, ct);
        if (!full.IsSuccess || full.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure(full.Error ?? "Pack not found");

        var built = await packService.BuildAsync(userId, new UrlAnalyzerResearchRequest
        {
            Keyword = pack.DerivedKeyword,
            Url = pack.SourceUrl,
            Location = pack.SearchLocation,
            BusinessContext = site.BusinessSummary,
        }, ct);

        if (!built.IsSuccess || built.Value is null)
        {
            var fail = SiteAnalyzerGateResult.Fail(built.Error ?? "Competitor crawl failed");
            return await FinishStepAsync(site.Id, pack.Id, 6, fail, log, null, ct);
        }

        var p = built.Value;
        var competitorCount = p.CompetitorOutlines.Count(c =>
            !string.IsNullOrWhiteSpace(c.H1) || c.Headings.Count > 0);
        log.Add($"Competitor outlines={p.CompetitorOutlines.Count}, with headings/H1={competitorCount}.");

        var existingWrite = UrlResearchEntityMapper.ToFullWrite(
            full.Value,
            full.Value.Status,
            full.Value.DataQuality ?? "partial");
        var crawledWrite = UrlResearchPackMapper.ToFullWrite(p, full.Value.Status);
        var mergedWrite = existingWrite with
        {
            Competitors = crawledWrite.Competitors,
            MedianWordCountTop5 = crawledWrite.MedianWordCountTop5,
            MedianTitleLengthTop10 = crawledWrite.MedianTitleLengthTop10,
            MedianH2CountTop5 = crawledWrite.MedianH2CountTop5,
            DominantContentFormat = crawledWrite.DominantContentFormat,
            DataQualityNotes = crawledWrite.DataQualityNotes,
        };
        await urlResearch.PersistFullAsync(pack.Id, mergedWrite, ct);

        var reloaded = await urlResearch.GetFullAsync(pack.Id, ct);
        var gate = reloaded.IsSuccess && reloaded.Value is not null
            ? SiteAnalyzerStepValidators.ValidatePackStep(6, reloaded.Value)
            : SiteAnalyzerGateResult.Fail("Could not reload pack after competitor persist.");

        return await FinishStepAsync(
            site.Id,
            pack.Id,
            6,
            gate,
            log,
            new Dictionary<string, int> { ["competitors"] = competitorCount },
            ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep7Async(
        Guid userId,
        GeekSeo.Persistence.Entities.SeoUrlResearch pack,
        GeekSeo.Persistence.Entities.SeoSiteResearch site,
        CancellationToken ct)
    {
        await ResetDownstreamPackStepRunsAsync(site.Id, pack.Id, fromStep: 8, ct);
        await MarkRunningAsync(site.Id, pack.Id, 7, ct);
        var built = await packService.BuildAsync(userId, new UrlAnalyzerResearchRequest
        {
            Keyword = pack.DerivedKeyword,
            Url = pack.SourceUrl,
            Location = pack.SearchLocation,
            BusinessContext = site.BusinessSummary,
        }, ct);
        if (!built.IsSuccess || built.Value is null)
            return await FinishStepAsync(site.Id, pack.Id, 7, SiteAnalyzerGateResult.Fail(built.Error ?? "Pack build failed"), [built.Error ?? ""], null, ct);

        var terms = built.Value.RecommendedTerms.Count;
        var gate = SiteAnalyzerStepValidators.ValidatePackStepFromBuild(7, built.Value);
        if (!gate.Passed)
            return await FinishStepAsync(site.Id, pack.Id, 7, gate, [$"Terms={terms}"], new Dictionary<string, int> { ["terms"] = terms }, ct);

        var write = UrlResearchPackMapper.ToFullWrite(built.Value, "running") with { Status = "running", DataQuality = "partial" };
        await urlResearch.PersistFullAsync(pack.Id, write, ct);

        return await FinishStepAsync(site.Id, pack.Id, 7, gate, [$"Terms={terms}"], new Dictionary<string, int> { ["terms"] = terms }, ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep8Async(
        Guid userId,
        GeekSeo.Persistence.Entities.SeoUrlResearch pack,
        GeekSeo.Persistence.Entities.SeoSiteResearch site,
        CancellationToken ct)
    {
        await ResetDownstreamPackStepRunsAsync(site.Id, pack.Id, fromStep: 9, ct);
        await MarkRunningAsync(site.Id, pack.Id, 8, ct);
        var built = await packService.BuildAsync(userId, new UrlAnalyzerResearchRequest
        {
            Keyword = pack.DerivedKeyword,
            Url = pack.SourceUrl,
            Location = pack.SearchLocation,
            BusinessContext = site.BusinessSummary,
        }, ct);
        if (!built.IsSuccess || built.Value is null)
            return await FinishStepAsync(site.Id, pack.Id, 8, SiteAnalyzerGateResult.Fail(built.Error ?? "Pack build failed"), [built.Error ?? ""], null, ct);

        var hints = built.Value.MethodologyHints.Count;
        var faqs = built.Value.ClosingFaqQuestions.Count;
        var gate = SiteAnalyzerStepValidators.ValidatePackStepFromBuild(8, built.Value);
        if (!gate.Passed)
        {
            return await FinishStepAsync(
                site.Id,
                pack.Id,
                8,
                gate,
                [$"Section hints={hints}, FAQs={faqs}"],
                new Dictionary<string, int> { ["sectionHints"] = hints, ["faqs"] = faqs },
                ct);
        }

        var write = UrlResearchPackMapper.ToFullWrite(built.Value, "running") with { Status = "running", DataQuality = "partial" };
        await urlResearch.PersistFullAsync(pack.Id, write, ct);

        return await FinishStepAsync(
            site.Id,
            pack.Id,
            8,
            gate,
            [$"Section hints={hints}, FAQs={faqs}"],
            new Dictionary<string, int> { ["sectionHints"] = hints, ["faqs"] = faqs },
            ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep9Async(
        Guid userId,
        GeekSeo.Persistence.Entities.SeoUrlResearch pack,
        GeekSeo.Persistence.Entities.SeoSiteResearch site,
        CancellationToken ct)
    {
        await ResetDownstreamPackStepRunsAsync(site.Id, pack.Id, fromStep: 10, ct);
        await MarkRunningAsync(site.Id, pack.Id, 9, ct);
        var full = await urlResearch.GetFullAsync(pack.Id, ct);
        if (!full.IsSuccess || full.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure(full.Error ?? "Pack not found");

        var mergedContext = string.IsNullOrWhiteSpace(site.BusinessSummary)
            ? full.Value.BusinessContext
            : $"{site.BusinessSummary} {full.Value.BusinessContext}".Trim();

        var gate = SiteAnalyzerStepValidators.ValidatePackStep9BeforePersist(mergedContext);
        if (!gate.Passed)
            return await FinishStepAsync(site.Id, pack.Id, 9, gate, [$"Merged context length={mergedContext.Length}"], null, ct);

        var rebuilt = await packService.BuildAsync(userId, new UrlAnalyzerResearchRequest
        {
            Keyword = full.Value.DerivedKeyword,
            Url = full.Value.SourceUrl,
            Location = full.Value.SearchLocation,
            BusinessContext = mergedContext,
        }, ct);
        if (!rebuilt.IsSuccess || rebuilt.Value is null)
        {
            var fail = SiteAnalyzerGateResult.Fail(rebuilt.Error ?? "Pack rebuild failed after context merge");
            return await FinishStepAsync(site.Id, pack.Id, 9, fail, [rebuilt.Error ?? "Pack rebuild failed"], null, ct);
        }

        var write = UrlResearchPackMapper.ToFullWrite(rebuilt.Value, "running") with
        {
            Status = "running",
            DataQuality = "partial",
            BusinessContext = mergedContext,
        };
        await urlResearch.PersistFullAsync(pack.Id, write, ct);

        return await FinishStepAsync(site.Id, pack.Id, 9, gate, [$"Merged context length={mergedContext.Length}"], null, ct);
    }

    private async Task<Result<SiteAnalyzerStepResponse>> RunStep10Async(
        Guid userId,
        GeekSeo.Persistence.Entities.SeoUrlResearch pack,
        GeekSeo.Persistence.Entities.SeoSiteResearch site,
        CancellationToken ct)
    {
        await MarkRunningAsync(site.Id, pack.Id, 10, ct);

        var full = await urlResearch.GetFullAsync(pack.Id, ct);
        if (!full.IsSuccess || full.Value is null)
            return Result<SiteAnalyzerStepResponse>.Failure(full.Error ?? "Pack not found");

        var now = DateTimeOffset.UtcNow;
        var write = UrlResearchEntityMapper.ToFullWrite(full.Value, "completed", "full", now);
        await urlResearch.PersistFullAsync(pack.Id, write, ct);
        await urlResearch.UpdateStatusAsync(pack.Id, new UrlResearchStatusPatch
        {
            Status = "completed",
            ResearchedAt = now,
        }, ct);

        return await FinishStepAsync(
            site.Id,
            pack.Id,
            10,
            SiteAnalyzerGateResult.Pass(),
            ["Ready for Content Writing."],
            null,
            ct);
    }

    private async Task MarkRunningAsync(Guid? siteId, Guid? packId, int step, CancellationToken ct) =>
        await siteResearch.UpsertStepRunAsync(new SiteAnalyzerStepRunUpsert
        {
            SiteResearchId = siteId,
            UrlResearchId = packId,
            StepNumber = step,
            Status = "running",
            Message = "Running…",
        }, ct);

    private async Task<Result<SiteAnalyzerStepResponse>> FinishStepAsync(
        Guid? siteId,
        Guid? packId,
        int step,
        SiteAnalyzerGateResult gate,
        IReadOnlyList<string> log,
        IReadOnlyDictionary<string, int>? counts,
        CancellationToken ct)
    {
        var status = gate.Passed ? "green" : "red";
        var logText = string.Join('\n', log);
        var countsJson = counts is null ? null : JsonSerializer.Serialize(counts, Json);

        var upsert = await siteResearch.UpsertStepRunAsync(new SiteAnalyzerStepRunUpsert
        {
            SiteResearchId = siteId,
            UrlResearchId = packId,
            StepNumber = step,
            Status = status,
            Message = gate.Passed ? $"Step {step} complete." : gate.Message,
            Log = logText,
            CountsJson = countsJson,
        }, ct);

        if (!upsert.IsSuccess)
        {
            logger.LogError(
                "Site Analyzer step {Step} run could not be saved: {Error}",
                step,
                upsert.Error);
            if (!gate.Passed)
                return Result<SiteAnalyzerStepResponse>.Failure(gate.Message);

            return Result<SiteAnalyzerStepResponse>.Failure(
                $"Step {step} completed but could not be saved: {upsert.Error}");
        }

        if (!gate.Passed)
            logger.LogWarning("Site Analyzer step {Step} red: {Message}", step, gate.Message);

        return Result<SiteAnalyzerStepResponse>.Success(CreateStepResponse(
            step,
            status,
            gate.Passed ? $"Step {step} complete." : gate.Message,
            logText,
            counts,
            gate.Passed ? null : gate.Message));
    }

    private static SiteAnalyzerStepResponse CreateStepResponse(
        int stepNumber,
        string status,
        string message,
        string log,
        IReadOnlyDictionary<string, int>? counts,
        string? validationMessage) =>
        new()
        {
            StepNumber = stepNumber,
            Status = status,
            Message = message,
            ValidationMessage = validationMessage,
            Log = log,
            Counts = counts,
        };

    private async Task<Result<SiteAnalyzerStepResponse>?> BlockIfPriorSiteStepsFailAsync(
        Guid siteId,
        Guid? packId,
        int step,
        GeekSeo.Persistence.Entities.SeoSiteResearch site,
        CancellationToken ct)
    {
        for (var s = 1; s < step; s++)
        {
            var check = SiteAnalyzerStepValidators.ValidateSiteIndexStep(s, site);
            if (check.Passed)
                continue;

            await FinishStepAsync(siteId, packId, s, check, [check.Message], null, ct);
            return Result<SiteAnalyzerStepResponse>.Failure(
                $"Step {s} must pass before running step {step}: {check.Message}");
        }

        return null;
    }

    private async Task<Result<SiteAnalyzerStepResponse>?> BlockIfPriorPackStepsFailAsync(
        Guid siteId,
        Guid packId,
        int step,
        GeekSeo.Persistence.Entities.SeoUrlResearch pack,
        CancellationToken ct)
    {
        var minStep = 5;
        for (var s = minStep; s < step; s++)
        {
            var check = SiteAnalyzerStepValidators.ValidatePackStep(s, pack);
            if (check.Passed)
                continue;

            await FinishStepAsync(siteId, packId, s, check, [check.Message], null, ct);
            return Result<SiteAnalyzerStepResponse>.Failure(
                $"Step {s} must pass before running step {step}: {check.Message}");
        }

        return null;
    }

    /// <summary>Heal a missing step-5 run row when SERP artifacts exist (legacy packs before step-run fix).</summary>
    private async Task ReconcilePackStepRunsAsync(
        Guid siteId,
        Guid packId,
        int throughStep,
        GeekSeo.Persistence.Entities.SeoUrlResearch pack,
        CancellationToken ct)
    {
        if (throughStep <= 5)
            return;

        var check = SiteAnalyzerStepValidators.ValidatePackStep(5, pack);
        if (!check.Passed)
            return;

        var runs = await siteResearch.GetStepRunsForPackAsync(packId, ct);
        var row = runs.Value?.FirstOrDefault(r => r.StepNumber == 5);
        if (row is not null && string.Equals(row.Status, "green", StringComparison.OrdinalIgnoreCase))
            return;

        var upsert = await siteResearch.UpsertStepRunAsync(new SiteAnalyzerStepRunUpsert
        {
            SiteResearchId = siteId,
            UrlResearchId = packId,
            StepNumber = 5,
            Status = "green",
            Message = "Step 5 complete.",
            Log = row?.Log ?? "Reconciled from persisted SERP artifacts.",
            CountsJson = row?.CountsJson,
        }, ct);

        if (!upsert.IsSuccess)
        {
            logger.LogWarning(
                "Could not reconcile Site Analyzer pack step 5 for {PackId}: {Error}",
                packId,
                upsert.Error);
        }
    }

    private async Task ResetDownstreamPackStepRunsAsync(
        Guid siteId,
        Guid packId,
        int fromStep,
        CancellationToken ct)
    {
        for (var s = fromStep; s <= 10; s++)
        {
            await siteResearch.UpsertStepRunAsync(new SiteAnalyzerStepRunUpsert
            {
                SiteResearchId = siteId,
                UrlResearchId = packId,
                StepNumber = s,
                Status = "pending",
                Message = string.Empty,
                Log = string.Empty,
            }, ct);
        }
    }

    private static IReadOnlyList<SiteAnalyzerStepResponse> BuildStepResponses(
        int fromStep,
        int toStep,
        IReadOnlyList<SiteAnalyzerStepRunRow> runs,
        Func<int, SiteAnalyzerGateResult>? validateStep = null)
    {
        var list = new List<SiteAnalyzerStepResponse>();
        for (var s = fromStep; s <= toStep; s++)
        {
            var row = runs.FirstOrDefault(r => r.StepNumber == s);
            var status = row?.Status ?? "pending";
            var message = row?.Message ?? string.Empty;

            if (validateStep is not null
                && row is not null
                && string.Equals(status, "green", StringComparison.OrdinalIgnoreCase))
            {
                var check = validateStep(s);
                if (!check.Passed)
                {
                    status = "red";
                    message = check.Message;
                }
            }

            list.Add(CreateStepResponse(
                s,
                status,
                message,
                row?.Log ?? string.Empty,
                ParseCounts(row?.CountsJson),
                string.Equals(status, "red", StringComparison.OrdinalIgnoreCase) ? message : null));
        }

        return list;
    }

    private static IReadOnlyDictionary<string, int>? ParseCounts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ParseUrlList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, Json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
