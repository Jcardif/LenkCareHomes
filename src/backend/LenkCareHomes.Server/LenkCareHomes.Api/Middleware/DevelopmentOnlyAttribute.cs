using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LenkCareHomes.Api.Middleware;

/// <summary>
///     Action filter that restricts access to development environment only.
///     Returns 404 Not Found in non-development environments to hide the endpoint's existence.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DevelopmentOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        if (!environment.IsDevelopment())
        {
            // Return 404 to hide the endpoint's existence in production
            context.Result = new NotFoundResult();
            return;
        }

        base.OnActionExecuting(context);
    }
}