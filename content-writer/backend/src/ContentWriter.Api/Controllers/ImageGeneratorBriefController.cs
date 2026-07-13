using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Figures;
using Microsoft.AspNetCore.Mvc;

namespace ContentWriter.Api.Controllers;

[ApiController]
[Route("api/image-generator")]
public class ImageGeneratorBriefController : ControllerBase
{
    private readonly IFigureDraftGenerationService _draftGeneration;
    private readonly ILogger<ImageGeneratorBriefController> _logger;

    public ImageGeneratorBriefController(
        IFigureDraftGenerationService draftGeneration,
        ILogger<ImageGeneratorBriefController> logger)
    {
        _draftGeneration = draftGeneration;
        _logger = logger;
    }

    [HttpPost("generate-from-brief")]
    public async Task<ActionResult<GenerateFromBriefResponse>> GenerateFromBrief(
        [FromBody] GenerateFromBriefRequest request,
        CancellationToken cancellationToken)
    {
        var heading = request.Heading?.Trim() ?? "";
        var brief = request.BriefText?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(brief))
        {
            return Problem("Brief text is required.", statusCode: 400, title: "Generate failed");
        }

        if (string.IsNullOrWhiteSpace(heading))
        {
            heading = "Section figure";
        }

        try
        {
            var avifBytes = await _draftGeneration.GenerateAvifFromBriefAsync(
                heading,
                brief,
                cancellationToken);

            var slug = Slugify(heading);
            return Ok(new GenerateFromBriefResponse(
                heading,
                $"{slug}-draft.avif",
                Convert.ToBase64String(avifBytes)));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Generate from brief failed");
            return Problem(ex.Message, statusCode: 400, title: "Generate failed");
        }
    }

    private static string Slugify(string heading)
    {
        var chars = heading
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? "figure" : slug;
    }
}

public sealed record GenerateFromBriefRequest(string? Heading, string? BriefText);

public sealed record GenerateFromBriefResponse(
    string Heading,
    string FileName,
    string ImageBase64);
