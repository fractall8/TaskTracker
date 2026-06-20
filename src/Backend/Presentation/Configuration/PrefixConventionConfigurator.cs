using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Presentation.Configuration;

public class PrefixConventionConfigurator(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefixRoute = new(new RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (!controller.Attributes.Any(a => a is ApiControllerAttribute))
            {
                continue;
            }

            foreach (var selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel != null)
                {
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                        _prefixRoute,
                        selector.AttributeRouteModel);
                }
                else
                {
                    selector.AttributeRouteModel = new AttributeRouteModel(_prefixRoute);
                }
            }
        }
    }
}
