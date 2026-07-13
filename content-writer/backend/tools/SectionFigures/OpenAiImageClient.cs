using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetEnv;
using SectionFigures.Models;

namespace SectionFigures;

public interface IOpenAiImageGenerator
{
    Task<byte[]> GeneratePngAsync(string prompt, CancellationToken cancellationToken = default);
}

public sealed class OpenAiImageClient(HttpClient http, string apiKey, string model, string size)
    : IOpenAiImageGenerator
{
    public static OpenAiImageClient FromEnvironment()
    {
        Env.TraversePath().Load();
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY is required for generate.");
        }

        var model = Environment.GetEnvironmentVariable("SECTION_FIGURES_OPENAI_MODEL")
            ?? ImageGenerationDefaults.OpenAiModel;
        var size = Environment.GetEnvironmentVariable("SECTION_FIGURES_OPENAI_SIZE")
            ?? ImageGenerationDefaults.OpenAiSize;

        return new OpenAiImageClient(new HttpClient(), apiKey, model, size);
    }

    public async Task<byte[]> GeneratePngAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var useLegacyResponseFormat = model.StartsWith("dall-e", StringComparison.OrdinalIgnoreCase);
        var payload = useLegacyResponseFormat
            ? (object)new { model, prompt, n = 1, size, response_format = "b64_json" }
            : new { model, prompt, n = 1, size };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/images/generations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(payload);

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI image generation failed ({(int)response.StatusCode}): {body}");
        }

        using var document = JsonDocument.Parse(body);
        var b64 = document.RootElement
            .GetProperty("data")[0]
            .GetProperty("b64_json")
            .GetString();

        if (string.IsNullOrWhiteSpace(b64))
        {
            throw new InvalidOperationException("OpenAI image response missing b64_json.");
        }

        return Convert.FromBase64String(b64);
    }
}

public enum FigureJobEventKind
{
    Started,
    SkippedExists,
    Succeeded,
    Failed,
}

public sealed record FigureJobEvent(
    FigureJobEventKind Kind,
    string RelativePath,
    string? Message);

public sealed record GenerateSummary(int Succeeded, int SkippedExists, int Failed);

public static class JobGenerator
{
    public static async Task<GenerateSummary> RunAsync(
        IReadOnlyList<FigureJob> jobs,
        string outputRoot,
        IOpenAiImageGenerator openAi,
        IFigureAvifEncoder avifEncoder,
        bool force,
        int concurrency,
        bool failFast,
        Action<FigureJobEvent>? onEvent = null,
        CancellationToken cancellationToken = default)
    {
        var succeeded = 0;
        var skippedExists = 0;
        var failed = 0;
        var gate = new SemaphoreSlim(Math.Max(1, concurrency));

        var tasks = jobs.Select(async job =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var absolute = JobPlanner.AbsolutePath(outputRoot, job.RelativePath);
                if (!force && File.Exists(absolute))
                {
                    Interlocked.Increment(ref skippedExists);
                    onEvent?.Invoke(new FigureJobEvent(FigureJobEventKind.SkippedExists, job.RelativePath, null));
                    Console.WriteLine($"SKIP (exists): {job.RelativePath}");
                    return;
                }

                try
                {
                    onEvent?.Invoke(new FigureJobEvent(FigureJobEventKind.Started, job.RelativePath, null));
                    var png = await openAi.GeneratePngAsync(job.ComposedPrompt, cancellationToken);
                    var avif = await avifEncoder.EncodePngAsync(png, cancellationToken);
                    await WriteAvifAtomicallyAsync(absolute, avif, cancellationToken);
                    Interlocked.Increment(ref succeeded);
                    onEvent?.Invoke(new FigureJobEvent(FigureJobEventKind.Succeeded, job.RelativePath, null));
                    Console.WriteLine($"OK: {job.RelativePath}");
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    onEvent?.Invoke(new FigureJobEvent(FigureJobEventKind.Failed, job.RelativePath, ex.Message));
                    Console.Error.WriteLine(
                        $"FAIL: {job.SourceType}/{job.HeadingSlug} ({job.Heading}): {ex.Message}");
                    if (failFast)
                    {
                        throw;
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
        return new GenerateSummary(succeeded, skippedExists, failed);
    }

    internal static async Task WriteAvifAtomicallyAsync(
        string absolutePath,
        byte[] avifBytes,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(absolutePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException($"Invalid path: {absolutePath}");
        }

        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $".{Path.GetFileName(absolutePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(tempPath, avifBytes, cancellationToken);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            File.Move(tempPath, absolutePath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
