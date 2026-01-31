using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelApp.Web.ViewComponents;

public class NavBarMenuViewComponent : ViewComponent
{
    private readonly INavMenuRepository _navMenuRepository;

    public NavBarMenuViewComponent(INavMenuRepository navMenuRepository)
    {
        _navMenuRepository = navMenuRepository;
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
            var all = await _navMenuRepository.GetAllActiveAsync();
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
            ? await _navMenuRepository.GetActiveForUserAsync(userId, isAdmin: false)
            : new List<NavMenuItem>();

        return View(items);
    }
}
