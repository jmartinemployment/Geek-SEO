using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetEnv;
using SectionFigures.Models;

namespace SectionFigures;

public sealed class ContentWriterFiguresClient(HttpClient http)
{
    public static ContentWriterFiguresClient FromEnvironment()
    {
        Env.TraversePath().Load();

        var baseUrl = Environment.GetEnvironmentVariable("CONTENT_WRITER_API_URL")?.TrimEnd('/')
            ?? throw new InvalidOperationException(
                "CONTENT_WRITER_API_URL is required (e.g. https://seo-api.geekatyourspot.com).");

        var client = new HttpClient { BaseAddress = new Uri(baseUrl + "/") };
        var apiKey = Environment.GetEnvironmentVariable("CONTENT_WRITER_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        return new ContentWriterFiguresClient(client);
    }

    public async Task<FigureManifestResponse> ExportManifestAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(
                $"api/projects/{projectId:D}/figures/export",
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Could not reach Content Writer API at {http.BaseAddress}: {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Project {projectId:D} not found on Content Writer API.");
        }

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Content Writer API auth failed — check CONTENT_WRITER_API_KEY.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Content Writer API returned {(int)response.StatusCode}: {body}");
        }

        var manifest = await response.Content.ReadFromJsonAsync<FigureManifestResponse>(
            cancellationToken: cancellationToken);
        if (manifest is null)
        {
            throw new InvalidOperationException("Empty response from figures/export.");
        }

        return manifest;
    }
}

public static class FigureJobBuilder
{
    public static FigureJobFile BuildJobs(FigureManifestResponse manifest)
    {
        if (manifest.Figures.Count == 0)
        {
            throw new InvalidOperationException(
                "No figures returned — run Step 6 (Generate briefs) in Content Writer first.");
        }

        var missingSlug = manifest.Figures
            .Where(f => string.IsNullOrWhiteSpace(f.GeekApiSlug))
            .ToList();
        if (missingSlug.Count > 0)
        {
            var names = string.Join(", ", missingSlug.Select(f => $"\"{f.Heading}\" ({f.SourceType}/{f.HeadingSlug})"));
            throw new InvalidOperationException(
                $"Publish text to geekatyourspot first. {missingSlug.Count} figure(s) missing GeekApiSlug: {names}");
        }

        var jobs = manifest.Figures
            .OrderBy(f => f.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.SectionOrder)
            .Select(f => new FigureJob(
                f.SourceType,
                f.HeadingSlug,
                f.Heading,
                f.SectionOrder,
                f.GeekApiSlug!.Trim(),
                f.BriefText,
                FigureImagePromptComposer.Compose(f.BriefText, f.Heading),
                FigurePublicPathBuilder.BuildRelativePath(f.GeekApiSlug!, f.HeadingSlug)))
            .ToList();

        return new FigureJobFile(
            manifest.ProjectId,
            DateTime.UtcNow.ToString("O"),
            jobs);
    }
}
