using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HotelApp.Web.Services;

namespace HotelApp.Web.Filters;

public class PageAuthorizationFilter : IAsyncActionFilter
{
    private readonly IAuthorizationMatrixService _authorization;

    public PageAuthorizationFilter(IAuthorizationMatrixService authorization)
    {
        _authorization = authorization;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        // Only enforce for authenticated users; unauthenticated is handled by [Authorize]/Cookie auth.
        if (http.User?.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var controller = (context.RouteData.Values["controller"]?.ToString() ?? string.Empty);
        var action = (context.RouteData.Values["action"]?.ToString() ?? string.Empty);

        // Skip Account (login/logout/denied) and static home.
        if (controller.Equals("Account", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Admin bypass (by username).
        var username = http.User.Identity?.Name ?? string.Empty;
        if (username.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var userId = http.Session.GetInt32("UserId") ?? 0;
        var branchId = http.Session.GetInt32("BranchID") ?? 0;

        if (userId <= 0)
        {
            await next();
            return;
        }

        var allowed = await _authorization.CanAccessPageAsync(userId, branchId, controller, action);
        if (!allowed)
        {
            var acceptsJson = http.Request.Headers.Accept.Any(h => h.Contains("application/json", StringComparison.OrdinalIgnoreCase));
            if (acceptsJson)
            {
                context.Result = new ForbidResult();
                return;
            }

            context.Result = new RedirectToActionResult("Denied", "Account", null);
            return;
        }

        await next();
    }
}
