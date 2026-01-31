namespace HotelApp.Web.Repositories;

public interface IAuthorizationPermissionRepository
{
    Task<IReadOnlyList<(int ResourceId, bool IsAllowed)>> GetRolePermissionsAsync(int roleId, int? branchId);
    Task<IReadOnlyList<(int ResourceId, bool IsAllowed)>> GetUserPermissionsAsync(int userId, int? branchId);

    Task UpsertRolePermissionsAsync(int roleId, int? branchId, IEnumerable<(int ResourceId, bool IsAllowed)> permissions, int? modifiedBy);
    Task UpsertUserPermissionsAsync(int userId, int? branchId, IEnumerable<(int ResourceId, bool IsAllowed)> permissions, int? modifiedBy);
}
