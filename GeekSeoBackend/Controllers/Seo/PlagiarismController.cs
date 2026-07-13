using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/plagiarism")]
public sealed class PlagiarismController(IPlagiarismProvider provider) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status() => Ok(new PlagiarismStatus(provider.IsConfigured, provider.ProviderName));
}
