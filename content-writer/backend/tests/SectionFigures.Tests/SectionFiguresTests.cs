using SectionFigures;
using SectionFigures.Models;

namespace SectionFigures.Tests;

public class FigurePublicPathBuilderTests
{
    [Theory]
    [InlineData("use-cases/accounting/smart-bank-reconciliation", "cost-allocation", "images/TechnicalArticle/accounting/smart-bank-reconciliation/h2-cost-allocation.avif")]
    [InlineData("blog/sales/quarterly-update", "intro", "images/Blog/sales/quarterly-update/h2-intro.avif")]
    [InlineData("tools/marketing/hubspot-ai", "capabilities", "images/Tool/marketing/hubspot-ai/h2-capabilities.avif")]
    public void BuildRelativePath_matches_geekatyourspot_convention(
        string geekApiSlug,
        string headingSlug,
        string expected)
    {
        Assert.Equal(expected, FigurePublicPathBuilder.BuildRelativePath(geekApiSlug, headingSlug));
    }
}

public class FigureJobBuilderTests
{
    [Fact]
    public void BuildJobs_includes_composed_prompt_and_path()
    {
        var manifest = new FigureManifestResponse(
            Guid.NewGuid(),
            [
                new FigureManifestEntry(
                    "pillar",
                    "overview",
                    "Overview",
                    1,
                    "Bold shapes showing workflow automation.",
                    "Pending",
                    null,
                    "use-cases/accounting/my-pillar"),
            ]);

        var file = FigureJobBuilder.BuildJobs(manifest);

        Assert.Single(file.Jobs);
        Assert.Contains("flat vector infographic", file.Jobs[0].ComposedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "images/TechnicalArticle/accounting/my-pillar/h2-overview.avif",
            file.Jobs[0].RelativePath);
    }

    [Fact]
    public void BuildJobs_fails_when_geekApiSlug_missing()
    {
        var manifest = new FigureManifestResponse(
            Guid.NewGuid(),
            [
                new FigureManifestEntry(
                    "pillar",
                    "overview",
                    "Overview",
                    1,
                    "Brief text here.",
                    "Pending",
                    null,
                    null),
            ]);

        var ex = Assert.Throws<InvalidOperationException>(() => FigureJobBuilder.BuildJobs(manifest));
        Assert.Contains("missing GeekApiSlug", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Overview", ex.Message);
    }
}

public class JobGeneratorFilesystemTests
{
    [Fact]
    public async Task RunAsync_skips_when_avif_exists()
    {
        var root = Path.Combine(Path.GetTempPath(), "section-figures-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var relative = "images/TechnicalArticle/accounting/page/h2-intro.avif";
            var absolute = JobPlanner.AbsolutePath(root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
            await File.WriteAllBytesAsync(absolute, [0x00, 0x01]);

            var jobs = new List<FigureJob>
            {
                new(
                    "pillar",
                    "intro",
                    "Intro",
                    1,
                    "use-cases/accounting/page",
                    "brief",
                    "prompt",
                    relative),
            };

            var fake = new FakeOpenAi(Array.Empty<byte>());
            var summary = await JobGenerator.RunAsync(jobs, root, fake, FakeAvifEncoder.Instance, force: false, concurrency: 1, failFast: false);

            Assert.Equal(0, summary.Succeeded);
            Assert.Equal(1, summary.SkippedExists);
            Assert.Equal(0, fake.CallCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_writes_when_missing()
    {
        var root = Path.Combine(Path.GetTempPath(), "section-figures-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var relative = "images/TechnicalArticle/accounting/page/h2-intro.avif";
            var jobs = new List<FigureJob>
            {
                new(
                    "pillar",
                    "intro",
                    "Intro",
                    1,
                    "use-cases/accounting/page",
                    "brief",
                    "prompt",
                    relative),
            };

            var png = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
            var fake = new FakeOpenAi(png);
            var summary = await JobGenerator.RunAsync(jobs, root, fake, FakeAvifEncoder.Instance, force: false, concurrency: 1, failFast: false);

            Assert.Equal(1, summary.Succeeded);
            Assert.True(File.Exists(JobPlanner.AbsolutePath(root, relative)));
            Assert.Equal(1, fake.CallCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FakeOpenAi(byte[] png) : IOpenAiImageGenerator
    {
        public int CallCount { get; private set; }

        public Task<byte[]> GeneratePngAsync(string prompt, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(png);
        }
    }

    private sealed class FakeAvifEncoder : IFigureAvifEncoder
    {
        public static FakeAvifEncoder Instance { get; } = new();

        public Task<byte[]> EncodePngAsync(byte[] pngBytes, CancellationToken cancellationToken = default) =>
            Task.FromResult(new byte[] { 0x00, 0x01, 0x02 });
    }
}
