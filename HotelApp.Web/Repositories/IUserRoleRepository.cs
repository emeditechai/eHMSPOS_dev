using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IUserRoleRepository
{
    Task<IEnumerable<UserRoleMapping>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Role>> GetRolesByUserIdAsync(int userId);
    Task AssignRolesToUserAsync(int userId, IEnumerable<int> roleIds, int assignedBy);
    Task RemoveRoleFromUserAsync(int userId, int roleId);
    Task<bool> HasRoleAsync(int userId, int roleId);
}
