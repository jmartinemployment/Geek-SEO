using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ProjectServiceUniquenessTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ExistingProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task CreateAsync_rejects_missing_default_location()
    {
        var repo = new FakeProjectRepository([]);
        var sut = new ProjectService(repo);

        var result = await sut.CreateAsync(UserId, new CreateProjectRequest
        {
            Name = "Test",
            Url = "https://example.com",
            DefaultLocation = "   ",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Default location is required.", result.Error);
        Assert.False(repo.CreateCalled);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_normalized_url_for_same_user()
    {
        var repo = new FakeProjectRepository([
            new SeoProject
            {
                Id = ExistingProjectId,
                UserId = UserId,
                Name = "Existing",
                Url = "https://example.com",
            },
        ]);
        var sut = new ProjectService(repo);

        var result = await sut.CreateAsync(UserId, new CreateProjectRequest
        {
            Name = "Duplicate",
            Url = "https://example.com/",
            DefaultLocation = "United States",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("A project for this website already exists.", result.Error);
        Assert.False(repo.CreateCalled);
    }

    [Fact]
    public async Task UpdateAsync_rejects_url_owned_by_another_project()
    {
        var repo = new FakeProjectRepository([
            new SeoProject
            {
                Id = ExistingProjectId,
                UserId = UserId,
                Name = "Existing",
                Url = "https://example.com",
            },
            new SeoProject
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = UserId,
                Name = "Other",
                Url = "https://other.com",
            },
        ]);
        var sut = new ProjectService(repo);

        var result = await sut.UpdateAsync(
            UserId,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            new UpdateProjectRequest { Url = "example.com" });

        Assert.False(result.IsSuccess);
        Assert.Equal("A project for this website already exists.", result.Error);
        Assert.False(repo.UpdateCalled);
    }

    [Fact]
    public async Task UpdateAsync_allows_same_project_to_keep_its_url()
    {
        var repo = new FakeProjectRepository([
            new SeoProject
            {
                Id = ExistingProjectId,
                UserId = UserId,
                Name = "Existing",
                Url = "https://example.com",
            },
        ]);
        var sut = new ProjectService(repo);

        var result = await sut.UpdateAsync(
            UserId,
            ExistingProjectId,
            new UpdateProjectRequest { Url = "https://example.com/" });

        Assert.True(result.IsSuccess);
        Assert.True(repo.UpdateCalled);
    }

    private sealed class FakeProjectRepository(IReadOnlyList<SeoProject> projects) : IProjectRepository
    {
        public bool CreateCalled { get; private set; }
        public bool UpdateCalled { get; private set; }

        public Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoProject>>.Success(projects.Where(p => p.UserId == userId).ToList()));

        public Task<Result<SeoProject>> GetByIdAsync(Guid projectId, CancellationToken ct = default)
        {
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            return Task.FromResult(project is null
                ? Result<SeoProject>.NotFound("Project not found")
                : Result<SeoProject>.Success(project));
        }

        public Task<Result<SeoProject>> GetByIdAsync(Guid projectId, Guid userId, CancellationToken ct = default) =>
            GetByIdAsync(projectId, ct);

        public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default)
        {
            CreateCalled = true;
            return Task.FromResult(Result<SeoProject>.Success(new SeoProject
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name,
                Url = request.Url,
            }));
        }

        public Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default)
        {
            UpdateCalled = true;
            var current = projects.First(p => p.Id == projectId);
            return Task.FromResult(Result<SeoProject>.Success(new SeoProject
            {
                Id = current.Id,
                UserId = current.UserId,
                OrgId = current.OrgId,
                Name = current.Name,
                Url = request.Url ?? current.Url,
                GscConnected = current.GscConnected,
                DefaultLocation = current.DefaultLocation,
                DefaultLanguage = current.DefaultLanguage,
                BusinessAddress = current.BusinessAddress,
                ServiceRadiusMiles = current.ServiceRadiusMiles,
                LocalSeoEnabled = current.LocalSeoEnabled,
                CreatedAt = current.CreatedAt,
                UpdatedAt = current.UpdatedAt,
            }));
        }

        public Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }
}
