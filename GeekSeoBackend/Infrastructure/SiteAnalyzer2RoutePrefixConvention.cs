using Microsoft.AspNetCore.Mvc.ApplicationModels;
using SiteAnalyzer2.Api.Controllers;

namespace GeekSeoBackend.Infrastructure;

/// <summary>
/// Mounts Site Analyzer 2 operator controllers under <c>/api/seo/sa2</c> (legacy in-repo wizard stays on <c>/api/seo/site-analyzer</c>).
/// </summary>
public sealed class SiteAnalyzer2RoutePrefixConvention : IApplicationModelConvention
{
    private static readonly HashSet<string> ExcludedControllers =
    [
        nameof(RunsController),
        nameof(InternalSerpRunsController),
    ];

    private readonly AttributeRouteModel _prefix = new(new Microsoft.AspNetCore.Mvc.RouteAttribute("api/seo/sa2"));

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ControllerType.Assembly != typeof(SitesController).Assembly)
                continue;

            if (ExcludedControllers.Contains(controller.ControllerName))
            {
                controller.Selectors.Clear();
                continue;
            }

            foreach (var selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel is null)
                {
                    selector.AttributeRouteModel = _prefix;
                    continue;
                }

                selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                    _prefix,
                    selector.AttributeRouteModel);
            }
        }
    }
}
