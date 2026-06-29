using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentDocumentKindResolverTests
{
    [Fact]
    public void Resolve_defaults_to_standalone_without_parent()
    {
        Assert.Equal(ContentDocumentKinds.Standalone, ContentDocumentKindResolver.Resolve(null, null));
    }

    [Fact]
    public void Resolve_defaults_to_spoke_when_parent_set()
    {
        Assert.Equal(
            ContentDocumentKinds.Spoke,
            ContentDocumentKindResolver.Resolve(null, Guid.Parse("11111111-1111-1111-1111-111111111111")));
    }

    [Fact]
    public void Resolve_honors_explicit_kind()
    {
        Assert.Equal(
            ContentDocumentKinds.Pillar,
            ContentDocumentKindResolver.Resolve(ContentDocumentKinds.Pillar, null));
    }
}
