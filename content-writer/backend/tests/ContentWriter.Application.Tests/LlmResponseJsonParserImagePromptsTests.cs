using ContentWriter.Application.Services;
using ContentWriter.Application.Providers;

namespace ContentWriter.Application.Tests;

public class LlmResponseJsonParserImagePromptsTests
{
    private static string BuildPrompt(int words) => string.Join(" ", Enumerable.Repeat("visual", words));

    [Fact]
    public void ParseImagePrompts_ValidJson_ReturnsDraft()
    {
        var pillarPrompt = BuildPrompt(80);
        var socialPrompt = BuildPrompt(60);
        var raw = $$"""
            {
              "pillarFigure": {
                "prompt": "{{pillarPrompt}}",
                "width": 1536,
                "height": 1024,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Illustration",
                "alchemy": true,
                "photoReal": false,
                "notes": "Flat vector infographic; avoid readable text."
              },
              "socialFacebook": {
                "prompt": "{{socialPrompt}}",
                "width": 1200,
                "height": 630,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Dynamic",
                "alchemy": true,
                "photoReal": false,
                "notes": "Eye-candy background only."
              },
              "socialLinkedIn": {
                "prompt": "{{socialPrompt}}",
                "width": 1200,
                "height": 630,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Dynamic",
                "alchemy": true,
                "photoReal": false,
                "notes": "Corporate polish."
              }
            }
            """;

        var draft = LlmResponseJsonParser.ParseImagePrompts(raw, "image prompts");

        Assert.Equal(1536, draft.PillarFigure.Width);
        Assert.Equal("Illustration", draft.PillarFigure.StylePreset);
        Assert.True(draft.PillarFigure.Alchemy);
        Assert.Equal(1200, draft.SocialLinkedIn.Width);
    }

    [Fact]
    public void ParseImagePrompts_PromptTooShort_Throws()
    {
        var shortPrompt = BuildPrompt(10);
        var raw = $$"""
            {
              "pillarFigure": {
                "prompt": "{{shortPrompt}}",
                "width": 1536,
                "height": 1024,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Illustration",
                "alchemy": true,
                "photoReal": false
              },
              "socialFacebook": {
                "prompt": "{{BuildPrompt(60)}}",
                "width": 1200,
                "height": 630,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Dynamic",
                "alchemy": true,
                "photoReal": false
              },
              "socialLinkedIn": {
                "prompt": "{{BuildPrompt(60)}}",
                "width": 1200,
                "height": 630,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Dynamic",
                "alchemy": true,
                "photoReal": false
              }
            }
            """;

        Assert.Throws<ContentGenerationException>(() =>
            LlmResponseJsonParser.ParseImagePrompts(raw, "image prompts"));
    }

    [Fact]
    public void ParseImagePrompts_SocialPortraitDimensions_Throws()
    {
        var raw = $$"""
            {
              "pillarFigure": {
                "prompt": "{{BuildPrompt(80)}}",
                "width": 1536,
                "height": 1024,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Illustration",
                "alchemy": true,
                "photoReal": false
              },
              "socialFacebook": {
                "prompt": "{{BuildPrompt(60)}}",
                "width": 630,
                "height": 1200,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Dynamic",
                "alchemy": true,
                "photoReal": false
              },
              "socialLinkedIn": {
                "prompt": "{{BuildPrompt(60)}}",
                "width": 1200,
                "height": 630,
                "leonardoModel": "Leonardo Phoenix",
                "stylePreset": "Dynamic",
                "alchemy": true,
                "photoReal": false
              }
            }
            """;

        Assert.Throws<ContentGenerationException>(() =>
            LlmResponseJsonParser.ParseImagePrompts(raw, "image prompts"));
    }
}
