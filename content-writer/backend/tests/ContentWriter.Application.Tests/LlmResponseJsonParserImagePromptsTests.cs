using ContentWriter.Application.Providers;
using ContentWriter.Application.Services;

namespace ContentWriter.Application.Tests;

public class LlmResponseJsonParserImagePromptsTests
{
    private static string BuildPrompt(int words) => string.Join(" ", Enumerable.Repeat("visual", words));

    private static readonly IReadOnlyList<ImagePromptSectionTarget> ExpectedSections =
    [
        new ImagePromptSectionTarget("pillar", "Why reconciliation matters", 1),
        new ImagePromptSectionTarget("blog", "Getting started", 1),
    ];

    [Fact]
    public void ParseSectionImagePrompts_ValidJson_ReturnsDraft()
    {
        var pillarPrompt = BuildPrompt(80);
        var blogPrompt = BuildPrompt(60);
        var raw = $$"""
            {
              "sections": [
                {
                  "sourceType": "pillar",
                  "heading": "Why reconciliation matters",
                  "order": 1,
                  "prompt": "{{pillarPrompt}}",
                  "width": 1536,
                  "height": 1024,
                  "notes": "Flat vector infographic; avoid readable text."
                },
                {
                  "sourceType": "blog",
                  "heading": "Getting started",
                  "order": 1,
                  "prompt": "{{blogPrompt}}",
                  "width": 1536,
                  "height": 1024,
                  "notes": "Warm step-by-step feel."
                }
              ]
            }
            """;

        var draft = LlmResponseJsonParser.ParseSectionImagePrompts(raw, ExpectedSections, "image prompts");

        Assert.Equal(2, draft.Sections.Count);
        Assert.Equal(1536, draft.Sections[0].Width);
        Assert.Equal("Flat vector infographic; avoid readable text.", draft.Sections[0].Notes);
        Assert.Equal("blog", draft.Sections[1].SourceType);
    }

    [Fact]
    public void ParseSectionImagePrompts_OmitsProviderFields_ReturnsDraft()
    {
        var pillarPrompt = BuildPrompt(80);
        var blogPrompt = BuildPrompt(60);
        var raw = $$"""
            {
              "sections": [
                {
                  "sourceType": "pillar",
                  "heading": "Why reconciliation matters",
                  "order": 1,
                  "prompt": "{{pillarPrompt}}",
                  "width": 1536,
                  "height": 1024
                },
                {
                  "sourceType": "blog",
                  "heading": "Getting started",
                  "order": 1,
                  "prompt": "{{blogPrompt}}",
                  "width": 1536,
                  "height": 1024
                }
              ]
            }
            """;

        var draft = LlmResponseJsonParser.ParseSectionImagePrompts(raw, ExpectedSections, "image prompts");

        Assert.Equal(2, draft.Sections.Count);
    }

    [Fact]
    public void ParseSectionImagePrompts_AdvertisementToolsSection_RequiresLongerBrief()
    {
        var sections = new List<ImagePromptSectionTarget>
        {
            new("pillar", "Choosing the Right Tools for Automated Transaction Categorization", 1),
        };
        var shortAdPrompt = BuildPrompt(ImagePromptDefaults.AdvertisementPromptMinWords - 1);
        var raw = $$"""
            {
              "sections": [
                {
                  "sourceType": "pillar",
                  "heading": "Choosing the Right Tools for Automated Transaction Categorization",
                  "order": 1,
                  "prompt": "{{shortAdPrompt}}",
                  "width": 1536,
                  "height": 1024
                }
              ]
            }
            """;

        Assert.Throws<ContentGenerationException>(() =>
            LlmResponseJsonParser.ParseSectionImagePrompts(raw, sections, "image prompts"));
    }

    [Fact]
    public void ParseSectionImagePrompts_AdvertisementToolsSection_AcceptsLongBrief()
    {
        var sections = new List<ImagePromptSectionTarget>
        {
            new("pillar", "Choosing the Right Tools for Automated Transaction Categorization", 1),
        };
        var adPrompt = BuildPrompt(ImagePromptDefaults.AdvertisementPromptMinWords);
        var raw = $$"""
            {
              "sections": [
                {
                  "sourceType": "pillar",
                  "heading": "Choosing the Right Tools for Automated Transaction Categorization",
                  "order": 1,
                  "prompt": "{{adPrompt}}",
                  "width": 1536,
                  "height": 1024
                }
              ]
            }
            """;

        var draft = LlmResponseJsonParser.ParseSectionImagePrompts(raw, sections, "image prompts");
        Assert.Single(draft.Sections);
    }

    [Fact]
    public void ParseSectionImagePrompts_PromptTooShort_Throws()
    {
        var raw = $$"""
            {
              "sections": [
                {
                  "sourceType": "pillar",
                  "heading": "Why reconciliation matters",
                  "order": 1,
                  "prompt": "{{BuildPrompt(10)}}",
                  "width": 1536,
                  "height": 1024
                },
                {
                  "sourceType": "blog",
                  "heading": "Getting started",
                  "order": 1,
                  "prompt": "{{BuildPrompt(60)}}",
                  "width": 1536,
                  "height": 1024
                }
              ]
            }
            """;

        Assert.Throws<ContentGenerationException>(() =>
            LlmResponseJsonParser.ParseSectionImagePrompts(raw, ExpectedSections, "image prompts"));
    }

    [Fact]
    public void ParseSectionImagePrompts_PortraitDimensions_Throws()
    {
        var raw = $$"""
            {
              "sections": [
                {
                  "sourceType": "pillar",
                  "heading": "Why reconciliation matters",
                  "order": 1,
                  "prompt": "{{BuildPrompt(80)}}",
                  "width": 1024,
                  "height": 1536
                },
                {
                  "sourceType": "blog",
                  "heading": "Getting started",
                  "order": 1,
                  "prompt": "{{BuildPrompt(60)}}",
                  "width": 1536,
                  "height": 1024
                }
              ]
            }
            """;

        Assert.Throws<ContentGenerationException>(() =>
            LlmResponseJsonParser.ParseSectionImagePrompts(raw, ExpectedSections, "image prompts"));
    }

    [Fact]
    public void ParseSectionImagePrompts_MissingSection_Throws()
    {
        var raw = $$"""
            {
              "sections": [
                {
                  "sourceType": "pillar",
                  "heading": "Why reconciliation matters",
                  "order": 1,
                  "prompt": "{{BuildPrompt(80)}}",
                  "width": 1536,
                  "height": 1024
                }
              ]
            }
            """;

        Assert.Throws<ContentGenerationException>(() =>
            LlmResponseJsonParser.ParseSectionImagePrompts(raw, ExpectedSections, "image prompts"));
    }
}
