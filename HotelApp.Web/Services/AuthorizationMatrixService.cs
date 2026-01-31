using HotelApp.Web.Repositories;

namespace HotelApp.Web.Services;

public class AuthorizationMatrixService : IAuthorizationMatrixService
{
    private readonly IAuthorizationResourceRepository _resourceRepository;
    private readonly IAuthorizationPermissionRepository _permissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public AuthorizationMatrixService(
        IAuthorizationResourceRepository resourceRepository,
        IAuthorizationPermissionRepository permissionRepository,
        IUserRoleRepository userRoleRepository)
    {
        _resourceRepository = resourceRepository;
        _permissionRepository = permissionRepository;
        _userRoleRepository = userRoleRepository;
    }

    // Default behavior: allow when no explicit rule exists.
    // (We only enforce deny/allow when permissions exist.)

    public async Task<bool> CanAccessPageAsync(int userId, int branchId, string controller, string action)
    {
        var resource = await _resourceRepository.GetPageResourceAsync(controller, action);
        if (resource is null)
        {
            return true;
        }

        var decision = await EvaluatePermissionAsync(userId, branchId, resource.Id);
        return decision ?? true;
    }

    public async Task<bool> CanAccessResourceKeyAsync(int userId, int branchId, string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return true;
        }

        var resource = await _resourceRepository.GetByKeyAsync(resourceKey);
        if (resource is null)
        {
            // If the key hasn't been registered yet, don't block the UI.
            return true;
        }

        var decision = await EvaluatePermissionAsync(userId, branchId, resource.Id);
        return decision ?? true;
    }

    private async Task<bool?> EvaluatePermissionAsync(int userId, int branchId, int resourceId)
    {
        // 1) User overrides (branch-specific, then global)
        var userBranchRules = await _permissionRepository.GetUserPermissionsAsync(userId, branchId);
        var userBranchRule = userBranchRules.FirstOrDefault(r => r.ResourceId == resourceId);
        if (userBranchRule != default)
        {
            return userBranchRule.IsAllowed;
        }

        var userGlobalRules = await _permissionRepository.GetUserPermissionsAsync(userId, null);
        var userGlobalRule = userGlobalRules.FirstOrDefault(r => r.ResourceId == resourceId);
        if (userGlobalRule != default)
        {
            return userGlobalRule.IsAllowed;
        }

        // 2) Role rules: deny wins, then allow
        var roles = (await _userRoleRepository.GetRolesByUserIdAsync(userId)).Select(r => r.Id).ToList();
        if (roles.Count == 0)
        {
            return null;
        }

        var roleBranchDecisions = new List<bool>();
        foreach (var roleId in roles)
        {
            var roleRules = await _permissionRepository.GetRolePermissionsAsync(roleId, branchId);
            foreach (var r in roleRules.Where(x => x.ResourceId == resourceId))
            {
                roleBranchDecisions.Add(r.IsAllowed);
            }
        }

        if (roleBranchDecisions.Any(x => x == false))
        {
            return false;
        }

        if (roleBranchDecisions.Any(x => x == true))
        {
            return true;
        }

        var roleGlobalDecisions = new List<bool>();
        foreach (var roleId in roles)
        {
            var roleRules = await _permissionRepository.GetRolePermissionsAsync(roleId, null);
            foreach (var r in roleRules.Where(x => x.ResourceId == resourceId))
            {
                roleGlobalDecisions.Add(r.IsAllowed);
            }
        }

        if (roleGlobalDecisions.Any(x => x == false))
        {
            return false;
        }

        if (roleGlobalDecisions.Any(x => x == true))
        {
            return true;
        }

        return null;
    }
}
