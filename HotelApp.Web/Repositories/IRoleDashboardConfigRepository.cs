using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IRoleDashboardConfigRepository
{
    Task<IEnumerable<RoleDashboardConfig>> GetAllWithRoleNamesAsync();
    Task<RoleDashboardConfig?> GetByRoleIdAsync(int roleId);
    Task UpdateAsync(RoleDashboardConfig config);
}
