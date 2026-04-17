using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelApp.Web.ViewComponents;

public class NavBarMenuViewComponent : ViewComponent
{
    private readonly INavMenuRepository _navMenuRepository;
    private readonly IRoleDashboardConfigRepository _roleDashboardConfigRepository;

    public NavBarMenuViewComponent(INavMenuRepository navMenuRepository, IRoleDashboardConfigRepository roleDashboardConfigRepository)
    {
        _navMenuRepository = navMenuRepository;
        _roleDashboardConfigRepository = roleDashboardConfigRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated == true;
        if (!isAuthenticated)
        {
            return View(new List<NavMenuItem>());
        }

        var username = HttpContext.User.Identity?.Name ?? string.Empty;
        var isAdmin = username.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        if (isAdmin)
        {
            // Admin user bypasses role-based filtering entirely.
            var all = (await _navMenuRepository.GetAllActiveAsync()).ToList();
            await RewriteHomeLinkAsync(all);
            return View(all);
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (userId <= 0)
        {
            var claimUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claimUserId, out var parsedId))
            {
                userId = parsedId;
            }
        }

        var items = userId > 0
            ? await _navMenuRepository.GetActiveForUserAsync(userId, isAdmin: false, selectedRoleId: HttpContext.Session.GetInt32("SelectedRoleId"))
            : new List<NavMenuItem>();

        await RewriteHomeLinkAsync(items);
        return View(items);
    }

    private async Task RewriteHomeLinkAsync(IEnumerable<NavMenuItem> items)
    {
        var roleId = HttpContext.Session.GetInt32("SelectedRoleId") ?? 0;
        if (roleId <= 0) return;

        var config = await _roleDashboardConfigRepository.GetByRoleIdAsync(roleId);
        if (config == null || !config.IsActive) return;

        // Find the Dashboard/Index nav item and point it at the role's configured dashboard
        var homeItem = items.FirstOrDefault(i =>
            string.Equals(i.Controller, "Dashboard", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(i.Action, "Index", StringComparison.OrdinalIgnoreCase));

        if (homeItem != null)
        {
            homeItem.Controller = config.DashboardController;
            homeItem.Action = config.DashboardAction;
        }
    }
}
