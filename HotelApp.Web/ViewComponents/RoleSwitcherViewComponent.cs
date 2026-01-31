using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelApp.Web.ViewComponents;

public class RoleSwitcherViewComponent : ViewComponent
{
    private readonly IUserRoleRepository _userRoleRepository;

    public RoleSwitcherViewComponent(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated == true;
        if (!isAuthenticated)
        {
            return Content(string.Empty);
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (userId <= 0)
        {
            var claimUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claimUserId, out userId))
            {
                return Content(string.Empty);
            }
        }

        var roles = (await _userRoleRepository.GetRolesByUserIdAsync(userId)).ToList();
        if (roles.Count <= 1)
        {
            return Content(string.Empty);
        }

        var selectedRoleId = HttpContext.Session.GetInt32("SelectedRoleId") ?? 0;
        var selectedRoleName = HttpContext.Session.GetString("SelectedRoleName")
                       ?? (HttpContext.User as ClaimsPrincipal)?.FindFirstValue("SelectedRoleName");

        return View(new HotelApp.Web.ViewModels.RoleSwitcherViewModel
        {
            Roles = roles,
            SelectedRoleName = selectedRoleName,
            SelectedRoleId = selectedRoleId
        });
    }
}
