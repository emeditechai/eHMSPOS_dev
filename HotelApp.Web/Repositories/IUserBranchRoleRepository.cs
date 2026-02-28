using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IUserBranchRoleRepository
{
    Task<IEnumerable<UserBranchRole>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Role>> GetRolesByUserBranchAsync(int userId, int branchId);
    Task SaveUserBranchRolesAsync(int userId, IEnumerable<UserBranchRoleAssignment> assignments, int? createdBy = null);
    Task<bool> HasRoleInBranchAsync(int userId, int branchId, int roleId);
}
