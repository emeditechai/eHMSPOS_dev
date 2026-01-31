using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface INavMenuRepository
{
    Task<IReadOnlyList<NavMenuItem>> GetAllActiveAsync();
    Task<IReadOnlyList<NavMenuItem>> GetActiveForUserAsync(int userId, bool isAdmin, int? selectedRoleId = null);
}
