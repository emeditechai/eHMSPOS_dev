using System.Security.Claims;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.ViewComponents;

public class UserProfileMenuViewComponent : ViewComponent
{
    private readonly IUserRoleRepository _userRoleRepository;

    public UserProfileMenuViewComponent(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var displayName = HttpContext.User.FindFirstValue("displayName")
                          ?? HttpContext.User.Identity?.Name
                          ?? "User";
        var email = HttpContext.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        var activeRoleName = HttpContext.Session.GetString("SelectedRoleName")
                             ?? HttpContext.User.FindFirstValue("SelectedRoleName")
                             ?? string.Empty;

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (userId <= 0)
        {
            var claimUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(claimUserId, out userId);
        }

        var canSwitchRole = false;
        if (userId > 0)
        {
            var roles = (await _userRoleRepository.GetRolesByUserIdAsync(userId)).ToList();
            canSwitchRole = roles.Count > 1;
        }

        return View(new UserProfileMenuViewModel
        {
            DisplayName = displayName,
            Email = email,
            ActiveRoleName = activeRoleName,
            CanSwitchRole = canSwitchRole,
            ProfilePicturePath = HttpContext.User.FindFirstValue("ProfilePicturePath")
        });
    }
}
