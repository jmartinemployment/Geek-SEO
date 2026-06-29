using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SiteAnalyzer2.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SerpWorkerAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = Environment.GetEnvironmentVariable("SERP_WORKER_API_KEY");
        if (string.IsNullOrWhiteSpace(expected))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            return;
        }

        var auth = context.HttpContext.Request.Headers.Authorization.ToString();
        if (auth != $"Bearer {expected}")
            context.Result = new UnauthorizedResult();
    }
}
