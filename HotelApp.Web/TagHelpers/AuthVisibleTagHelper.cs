using HotelApp.Web.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HotelApp.Web.TagHelpers;

[HtmlTargetElement(Attributes = "auth-key")]
public class AuthVisibleTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationMatrixService _authorization;

    public AuthVisibleTagHelper(IHttpContextAccessor httpContextAccessor, IAuthorizationMatrixService authorization)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorization = authorization;
    }

    [HtmlAttributeName("auth-key")]
    public string AuthKey { get; set; } = string.Empty;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var username = http.User.Identity?.Name ?? string.Empty;
        if (username.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var userId = http.Session.GetInt32("UserId") ?? 0;
        var branchId = http.Session.GetInt32("BranchID") ?? 0;
        var selectedRoleId = http.Session.GetInt32("SelectedRoleId");
        if (userId <= 0)
        {
            return;
        }

        var allowed = await _authorization.CanAccessResourceKeyAsync(userId, branchId, AuthKey, selectedRoleId);
        if (!allowed)
        {
            output.SuppressOutput();
        }
    }
}
