using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using HotelApp.Web.Models;

namespace HotelApp.Web.Controllers;

[Authorize]
public class AuthorizationMatrixController : BaseController
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IAuthorizationResourceRepository _resourceRepository;
    private readonly IAuthorizationPermissionRepository _permissionRepository;

    public AuthorizationMatrixController(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IBranchRepository branchRepository,
        IAuthorizationResourceRepository resourceRepository,
        IAuthorizationPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _branchRepository = branchRepository;
        _resourceRepository = resourceRepository;
        _permissionRepository = permissionRepository;
    }

    private bool IsAdminUser() => (User?.Identity?.Name ?? string.Empty).Equals("Admin", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Index(string scopeType = "Role", int? scopeId = null, int? branchId = null)
    {
        if (!IsAdminUser())
        {
            return Forbid();
        }

        var roles = (await _roleRepository.GetAllRolesAsync()).ToList();
        var users = (await _userRepository.GetAllUsersAsync()).ToList();
        var branches = (await _branchRepository.GetActiveBranchesAsync()).ToList();

        var effectiveScopeType = (scopeType.Equals("User", StringComparison.OrdinalIgnoreCase)) ? "User" : "Role";

        var selectedScopeId = scopeId
            ?? (effectiveScopeType == "User" ? users.FirstOrDefault()?.Id : roles.FirstOrDefault()?.Id)
            ?? 0;

        var resources = (await _resourceRepository.GetAllActiveAsync()).ToList();

        // Load existing decisions
        var decisionByResourceId = new Dictionary<int, string>();
        if (selectedScopeId > 0)
        {
            if (effectiveScopeType == "Role")
            {
                var scoped = await _permissionRepository.GetRolePermissionsAsync(selectedScopeId, branchId);
                var global = branchId is null ? Array.Empty<(int ResourceId, bool IsAllowed)>() : await _permissionRepository.GetRolePermissionsAsync(selectedScopeId, null);

                foreach (var (rid, allow) in global)
                {
                    decisionByResourceId[rid] = allow ? "Allow" : "Deny";
                }

                foreach (var (rid, allow) in scoped)
                {
                    decisionByResourceId[rid] = allow ? "Allow" : "Deny";
                }
            }
            else
            {
                var scoped = await _permissionRepository.GetUserPermissionsAsync(selectedScopeId, branchId);
                var global = branchId is null ? Array.Empty<(int ResourceId, bool IsAllowed)>() : await _permissionRepository.GetUserPermissionsAsync(selectedScopeId, null);

                foreach (var (rid, allow) in global)
                {
                    decisionByResourceId[rid] = allow ? "Allow" : "Deny";
                }

                foreach (var (rid, allow) in scoped)
                {
                    decisionByResourceId[rid] = allow ? "Allow" : "Deny";
                }
            }
        }

        var tree = BuildTree(resources, decisionByResourceId);

        var model = new AuthorizationMatrixViewModel
        {
            ScopeType = effectiveScopeType,
            ScopeId = selectedScopeId,
            BranchId = branchId,
            Roles = roles,
            Users = users,
            Branches = branches,
            ResourceTree = tree
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AuthorizationMatrixViewModel model, List<int> resourceIds, List<string> decisions)
    {
        if (!IsAdminUser())
        {
            return Forbid();
        }

        var branchId = model.BranchId;
        var scopeType = model.ScopeType.Equals("User", StringComparison.OrdinalIgnoreCase) ? "User" : "Role";

        var merged = new List<(int ResourceId, bool IsAllowed)>();
        for (var i = 0; i < Math.Min(resourceIds.Count, decisions.Count); i++)
        {
            var decision = decisions[i];
            if (decision.Equals("Allow", StringComparison.OrdinalIgnoreCase))
            {
                merged.Add((resourceIds[i], true));
            }
            else if (decision.Equals("Deny", StringComparison.OrdinalIgnoreCase))
            {
                merged.Add((resourceIds[i], false));
            }
        }

        if (scopeType == "Role")
        {
            await _permissionRepository.UpsertRolePermissionsAsync(model.ScopeId, branchId, merged, CurrentUserId);
        }
        else
        {
            await _permissionRepository.UpsertUserPermissionsAsync(model.ScopeId, branchId, merged, CurrentUserId);
        }

        TempData["SuccessMessage"] = "Authorization saved.";
        return RedirectToAction(nameof(Index), new { scopeType = model.ScopeType, scopeId = model.ScopeId, branchId = model.BranchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUiResource(AuthorizationMatrixViewModel model)
    {
        if (!IsAdminUser())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(model.NewUiKey) || string.IsNullOrWhiteSpace(model.NewUiTitle))
        {
            TempData["ErrorMessage"] = "UI Key and Title are required.";
            return RedirectToAction(nameof(Index), new { scopeType = model.ScopeType, scopeId = model.ScopeId, branchId = model.BranchId });
        }

        var key = model.NewUiKey.Trim();
        if (!key.StartsWith("UI:", StringComparison.OrdinalIgnoreCase))
        {
            key = "UI:" + key;
        }

        await _resourceRepository.CreateUiResourceAsync(key, model.NewUiTitle.Trim(), model.NewUiParentResourceId, sortOrder: 999, createdBy: CurrentUserId);
        TempData["SuccessMessage"] = "UI resource added.";

        return RedirectToAction(nameof(Index), new { scopeType = model.ScopeType, scopeId = model.ScopeId, branchId = model.BranchId });
    }

    private static List<ResourceNodeVm> BuildTree(List<AuthorizationResource> resources, Dictionary<int, string> decisionById)
    {
        var byParent = resources
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Title)
            .GroupBy(r => r.ParentResourceId ?? 0)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<ResourceNodeVm> Build(int parentId)
        {
            if (!byParent.TryGetValue(parentId, out var children))
            {
                return new List<ResourceNodeVm>();
            }

            return children.Select(r => new ResourceNodeVm
            {
                Id = r.Id,
                ResourceType = r.ResourceType,
                ResourceKey = r.ResourceKey,
                Title = r.Title,
                Controller = r.Controller,
                Action = r.Action,
                SortOrder = r.SortOrder,
                Decision = decisionById.TryGetValue(r.Id, out var d) ? d : "Inherit",
                Children = Build(r.Id)
            }).ToList();
        }

        return Build(0);
    }
}
