using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HotelApp.Web.Filters;

/// <summary>
/// Global action filter that validates every authenticated request has a valid
/// Branch and Role in session (or claims). If either is missing the user is
/// signed out and redirected to the Login page so they must go through the full
/// Branch → Role selection process again.
/// </summary>
public class SessionValidationFilter : IAsyncActionFilter
{
    // Controllers that are always allowed through without a full session.
    private static readonly HashSet<string> _skippedControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Account",
        "GuestFeedback",   // public feedback pages
        "Home",
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        // Skip if user is not authenticated – cookie auth / [Authorize] handles that.
        if (http.User?.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        if (_skippedControllers.Contains(controller))
        {
            await next();
            return;
        }

        // ------------------------------------------------------------------
        // Validate that BranchID is set (session or claim).
        // ------------------------------------------------------------------
        var branchId = http.Session.GetInt32("BranchID")
            ?? (int.TryParse(http.User.FindFirst("BranchID")?.Value, out var cb) && cb > 0 ? cb : 0);

        // ------------------------------------------------------------------
        // Validate that SelectedRoleName is set (session or claim).
        // ------------------------------------------------------------------
        var roleName = http.Session.GetString("SelectedRoleName")
            ?? http.User.FindFirst("SelectedRoleName")?.Value
            ?? string.Empty;

        // ------------------------------------------------------------------
        // Also validate SelectedRoleId (session or claim).
        // ------------------------------------------------------------------
        var roleId = http.Session.GetInt32("SelectedRoleId")
            ?? (int.TryParse(http.User.FindFirst("SelectedRoleId")?.Value, out var cr) && cr > 0 ? cr : 0);

        bool sessionValid = branchId > 0 && !string.IsNullOrWhiteSpace(roleName) && roleId > 0;

        if (!sessionValid)
        {
            // Session is incomplete — sign the user out fully and redirect to login.
            http.Session.Clear();
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Detect AJAX / JSON requests and return 401 instead of a redirect.
            var acceptHeader = http.Request.Headers.Accept.ToString();
            var isAjax = http.Request.Headers.XRequestedWith == "XMLHttpRequest"
                      || acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (isAjax)
            {
                context.Result = new UnauthorizedResult();
            }
            else
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    new { returnUrl = (string?)null, reason = "session" });
            }

            return;
        }

        // Re-hydrate session from claims if any key was missing from session
        // (e.g., after a server hot-reload that cleared in-memory session).
        if (http.Session.GetInt32("BranchID") is null && branchId > 0)
            http.Session.SetInt32("BranchID", branchId);
        if (string.IsNullOrWhiteSpace(http.Session.GetString("SelectedRoleName")) && !string.IsNullOrWhiteSpace(roleName))
            http.Session.SetString("SelectedRoleName", roleName);
        if (http.Session.GetInt32("SelectedRoleId") is null && roleId > 0)
            http.Session.SetInt32("SelectedRoleId", roleId);

        await next();
    }
}
