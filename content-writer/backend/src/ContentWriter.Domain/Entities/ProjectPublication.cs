using ContentWriter.Domain.Enums;

namespace ContentWriter.Domain.Entities;

public class ProjectPublication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public GeneratedContentType ContentType { get; set; }
    public int GeekPostId { get; set; }
    public string GeekApiSlug { get; set; } = string.Empty;
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
}
