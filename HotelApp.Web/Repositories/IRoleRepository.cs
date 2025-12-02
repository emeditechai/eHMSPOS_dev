using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IRoleRepository
{
    Task<IEnumerable<Role>> GetAllRolesAsync();
    Task<Role?> GetByIdAsync(int id);
}
