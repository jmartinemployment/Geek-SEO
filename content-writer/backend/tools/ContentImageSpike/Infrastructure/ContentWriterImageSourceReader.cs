using ContentImageSpike.Abstractions;
using ContentImageSpike.Domain;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContentImageSpike.Infrastructure;

public sealed class ContentWriterImageSourceReader : IContentImageSourceReader
{
    private readonly ContentWriterDbContext _db;

    public ContentWriterImageSourceReader(ContentWriterDbContext db) => _db = db;

    public async Task<ContentImageSource?> LoadAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects
            .AsNoTracking()
            .Include(p => p.GeneratedContents)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return null;

        var pillar = project.GeneratedContents
            .FirstOrDefault(c => c.ContentType == GeneratedContentType.TechnicalArticle);

        var facebook = project.GeneratedContents
            .FirstOrDefault(c => c.ContentType == GeneratedContentType.SocialFacebook);

        var linkedIn = project.GeneratedContents
            .FirstOrDefault(c => c.ContentType == GeneratedContentType.SocialLinkedIn);

        return new ContentImageSource(
            project.Id,
            project.Name,
            project.TargetKeyword,
            DetectedTone: null,
            Pillar: pillar is null ? null : ToPillarBrief(pillar),
            Facebook: facebook is null ? null : ToSocialBrief("Facebook", facebook),
            LinkedIn: linkedIn is null ? null : ToSocialBrief("LinkedIn", linkedIn));
    }

    private static PillarImageBrief ToPillarBrief(ContentWriter.Domain.Entities.GeneratedContent pillar) =>
        new(
            pillar.Title,
            pillar.MetaDescription ?? string.Empty,
            pillar.Keywords,
            pillar.SectionOutline.Take(5).ToList());

    private static SocialImageBrief ToSocialBrief(
        string platform,
        ContentWriter.Domain.Entities.GeneratedContent social) =>
        new(
            platform,
            HtmlTextHelper.StripHtml(social.BodyHtml),
            social.RelatedArticleUrl);
}
