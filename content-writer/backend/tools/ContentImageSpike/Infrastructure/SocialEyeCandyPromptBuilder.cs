using ContentImageSpike.Abstractions;
using ContentImageSpike.Domain;

namespace ContentImageSpike.Infrastructure;

/// <summary>Eye-candy card backgrounds for social posts — no readable text in the image.</summary>
public sealed class SocialEyeCandyPromptBuilder : IImagePromptBuilder
{
  private readonly ImageUseCase _useCase;
  private readonly string _platformLabel;

  public SocialEyeCandyPromptBuilder(ImageUseCase useCase, string platformLabel)
  {
    _useCase = useCase;
    _platformLabel = platformLabel;
  }

  public ImageUseCase UseCase => _useCase;

  public ImageGenerationRequest Build(ContentImageSource source)
  {
    var brief = _useCase switch
    {
      ImageUseCase.SocialFacebook => source.Facebook,
      ImageUseCase.SocialLinkedIn => source.LinkedIn,
      _ => throw new InvalidOperationException($"Use case {_useCase} is not a social platform."),
    };

    if (brief is null)
      throw new InvalidOperationException($"Project has no {_platformLabel} social content.");

    var snippet = HtmlTextHelper.Truncate(brief.PostText, 220);
    var topic = string.IsNullOrWhiteSpace(source.TargetKeyword)
      ? source.Pillar?.Title ?? source.ProjectName
      : source.TargetKeyword;

    var mood = _useCase == ImageUseCase.SocialLinkedIn
      ? "polished corporate tech aesthetic, subtle gradients, abstract geometric shapes"
      : "approachable B2B tech vibe, warmer accent colors, dynamic abstract shapes";

    var prompt = $"""
      Eye-catching social media card background for {_platformLabel} about "{topic}".
      Post context (do not render as text): {snippet}.
      Style: {mood}, high contrast, space for headline text overlay in post-production.
      No readable words, letters, logos, or watermarks in the image.
      Modern, scroll-stopping, professional IT services marketing visual.
      """;

    return new ImageGenerationRequest(_useCase, prompt.Trim(), Width: 1200, Height: 630);
  }
}
