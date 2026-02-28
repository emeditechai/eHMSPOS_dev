using HotelApp.Web.Repositories;

namespace HotelApp.Web.Services;

public class AuthorizationMatrixService : IAuthorizationMatrixService
{
    private readonly IAuthorizationResourceRepository _resourceRepository;
    private readonly IAuthorizationPermissionRepository _permissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserBranchRoleRepository _userBranchRoleRepository;

    public AuthorizationMatrixService(
        IAuthorizationResourceRepository resourceRepository,
        IAuthorizationPermissionRepository permissionRepository,
        IUserRoleRepository userRoleRepository,
        IUserBranchRoleRepository userBranchRoleRepository)
    {
        _resourceRepository = resourceRepository;
        _permissionRepository = permissionRepository;
        _userRoleRepository = userRoleRepository;
        _userBranchRoleRepository = userBranchRoleRepository;
    }

    // Default behavior: allow when no explicit rule exists.
    // (We only enforce deny/allow when permissions exist.)

    public async Task<bool> CanAccessPageAsync(int userId, int branchId, string controller, string action, int? selectedRoleId = null)
    {
        var resource = await _resourceRepository.GetPageResourceAsync(controller, action);
        if (resource is null)
        {
            return true;
        }

        var decision = await EvaluatePermissionAsync(userId, branchId, resource.Id, selectedRoleId);
        return decision ?? true;
    }

    public async Task<bool> CanAccessResourceKeyAsync(int userId, int branchId, string resourceKey, int? selectedRoleId = null)
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

        var decision = await EvaluatePermissionAsync(userId, branchId, resource.Id, selectedRoleId);
        return decision ?? true;
    }

    private async Task<bool?> EvaluatePermissionAsync(int userId, int branchId, int resourceId, int? selectedRoleId)
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

        // 2) Role rules: when selectedRoleId is provided, evaluate only that role.
        // Otherwise evaluate across all roles (deny wins, then allow).
        // Use branch-specific roles, fall back to global roles if none assigned
        var branchRolesList = (await _userBranchRoleRepository.GetRolesByUserBranchAsync(userId, branchId)).ToList();
        var roles = branchRolesList.Any()
            ? branchRolesList.Select(r => r.Id).ToList()
            : (await _userRoleRepository.GetRolesByUserIdAsync(userId)).Select(r => r.Id).ToList();
        if (roles.Count == 0)
        {
            return null;
        }

        if (selectedRoleId.HasValue && selectedRoleId.Value > 0 && roles.Contains(selectedRoleId.Value))
        {
            roles = new List<int> { selectedRoleId.Value };
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
