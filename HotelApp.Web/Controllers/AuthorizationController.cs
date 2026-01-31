using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using HotelApp.Web.Models;

namespace HotelApp.Web.Controllers;

[Authorize]
public class AuthorizationController : BaseController
{
    private readonly IRoleRepository _roleRepository;
    private readonly INavMenuRepository _navMenuRepository;
    private readonly IRoleNavMenuRepository _roleNavMenuRepository;

    public AuthorizationController(
        IRoleRepository roleRepository,
        INavMenuRepository navMenuRepository,
        IRoleNavMenuRepository roleNavMenuRepository)
    {
        _roleRepository = roleRepository;
        _navMenuRepository = navMenuRepository;
        _roleNavMenuRepository = roleNavMenuRepository;
    }

    private bool IsAdminUser()
    {
        var username = User?.Identity?.Name ?? string.Empty;
        return username.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> RoleMenuMapping(int? roleId)
    {
        if (!IsAdminUser())
        {
            return Forbid();
        }

        var roles = (await _roleRepository.GetAllRolesAsync()).ToList();
        if (roles.Count == 0)
        {
            return View(new RoleMenuMappingViewModel());
        }

        var selectedRoleId = roleId ?? roles.First().Id;

        var allMenus = (await _navMenuRepository.GetAllActiveAsync()).ToList();
        var assignedIds = (await _roleNavMenuRepository.GetActiveMenuIdsByRoleAsync(selectedRoleId)).ToHashSet();

        var tree = BuildTree(allMenus, assignedIds);

        var model = new RoleMenuMappingViewModel
        {
            RoleId = selectedRoleId,
            Roles = roles,
            MenuTree = tree,
            SelectedMenuIds = assignedIds.ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RoleMenuMapping(RoleMenuMappingViewModel model)
    {
        if (!IsAdminUser())
        {
            return Forbid();
        }

        await _roleNavMenuRepository.SaveRoleMenusAsync(model.RoleId, model.SelectedMenuIds ?? Enumerable.Empty<int>(), CurrentUserId);
        TempData["SuccessMessage"] = "Role menu mapping saved.";
        return RedirectToAction(nameof(RoleMenuMapping), new { roleId = model.RoleId });
    }

    private static List<MenuNodeViewModel> BuildTree(List<NavMenuItem> allMenus, HashSet<int> assignedIds)
    {
        var byParent = allMenus
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .GroupBy(x => x.ParentId ?? 0)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<MenuNodeViewModel> Build(int parentId)
        {
            if (!byParent.TryGetValue(parentId, out var children))
            {
                return new List<MenuNodeViewModel>();
            }

            return children.Select(m => new MenuNodeViewModel
            {
                Id = m.Id,
                Title = m.Title,
                IconClass = m.IconClass,
                Controller = m.Controller,
                Action = m.Action,
                SortOrder = m.SortOrder,
                IsAssigned = assignedIds.Contains(m.Id),
                Children = Build(m.Id)
            }).ToList();
        }

        return Build(0);
    }
}
