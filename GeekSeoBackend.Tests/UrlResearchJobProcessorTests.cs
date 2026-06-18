using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeekSeoBackend.Tests;

public sealed class UrlResearchJobProcessorTests
{
    private static readonly Guid ResearchId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task ProcessBatchAsync_marks_failed_when_persist_fails()
    {
        var repo = new FakeUrlResearchRepository();
        var progress = new RecordingProgressNotifier();
        var analyze = new FakeAnalyzeRunner(Result<UrlResearchFullWrite>.Success(SampleWrite()));
        var processor = new UrlResearchJobProcessor(
            repo,
            analyze,
            progress,
            new WorkerUserContext(),
            NullLogger<UrlResearchJobProcessor>.Instance);

        await processor.ProcessBatchAsync();

        Assert.Equal("failed", repo.LastStatusPatch?.Status);
        Assert.Equal("persist denied", repo.LastStatusPatch?.ErrorMessage);
        Assert.Contains(progress.Pushes, p => p.Status == "running");
        Assert.Contains(progress.Pushes, p => p.Status == "failed" && p.ErrorMessage == "persist denied");
    }

    [Fact]
    public async Task ProcessBatchAsync_pushes_completed_when_persist_succeeds()
    {
        var repo = new FakeUrlResearchRepository { PersistSucceeds = true };
        var progress = new RecordingProgressNotifier();
        var analyze = new FakeAnalyzeRunner(Result<UrlResearchFullWrite>.Success(SampleWrite()));
        var processor = new UrlResearchJobProcessor(
            repo,
            analyze,
            progress,
            new WorkerUserContext(),
            NullLogger<UrlResearchJobProcessor>.Instance);

        await processor.ProcessBatchAsync();

        Assert.Null(repo.LastStatusPatch);
        Assert.Contains(progress.Pushes, p => p.Status == "completed");
    }

    private static UrlResearchFullWrite SampleWrite() => new()
    {
        DerivedKeyword = "widget repair",
        SearchLocation = "United States",
        Status = "completed",
        IntentPrimary = "informational",
        IntentJustification = "guide",
        PafType = "paragraph",
        PafFormat = "text",
        DirectAnswerInstruction = "Answer directly.",
        DominantContentFormat = "guide",
    };

    private sealed class FakeAnalyzeRunner(Result<UrlResearchFullWrite> result) : IUrlResearchAnalyzeRunner
    {
        public Task<Result<UrlResearchFullWrite>> BuildFullWriteAsync(
            Guid userId, Guid projectId, string sourceUrl, CancellationToken ct = default) =>
            Task.FromResult(result);
    }

    private sealed class RecordingProgressNotifier : IUrlResearchProgressNotifier
    {
        public List<(string Status, string? ErrorMessage)> Pushes { get; } = [];

        public Task PushAsync(
            Guid urlResearchId,
            Guid projectId,
            Guid userId,
            string status,
            string? message = null,
            string? errorMessage = null,
            CancellationToken ct = default)
        {
            Pushes.Add((status, errorMessage));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUrlResearchRepository : IUrlResearchRepository
    {
        public bool PersistSucceeds { get; init; }
        public UrlResearchStatusPatch? LastStatusPatch { get; private set; }

        public Task<Result<IReadOnlyList<UrlResearchQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<UrlResearchQueuedJob>>.Success(
            [
                new UrlResearchQueuedJob(ResearchId, ProjectId, UserId, "https://example.com/page"),
            ]));

        public Task<Result<bool>> TryClaimRunningAsync(Guid urlResearchId, CancellationToken ct = default) =>
            Task.FromResult(Result<bool>.Success(true));

        public Task<Result<SeoUrlResearch>> PersistFullAsync(
            Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default) =>
            PersistSucceeds
                ? Task.FromResult(Result<SeoUrlResearch>.Success(new SeoUrlResearch
                {
                    Id = urlResearchId,
                    ProjectId = ProjectId,
                    UserId = UserId,
                    SourceUrl = "https://example.com/page",
                    Status = "completed",
                }))
                : Task.FromResult(Result<SeoUrlResearch>.Failure("persist denied"));

        public Task<Result<SeoUrlResearch>> UpdateStatusAsync(
            Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default)
        {
            LastStatusPatch = patch;
            return Task.FromResult(Result<SeoUrlResearch>.Success(new SeoUrlResearch
            {
                Id = urlResearchId,
                ProjectId = ProjectId,
                UserId = UserId,
                SourceUrl = "https://example.com/page",
                Status = patch.Status,
            }));
        }

        public Task<Result<SeoUrlResearch>> CreateQueuedAsync(
            Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> GetHeadAsync(Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> GetFullAsync(Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(
            Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<int>> FailStaleRunningAsync(TimeSpan maxAge, CancellationToken ct = default) =>
            Task.FromResult(Result<int>.Success(0));
    }
}
