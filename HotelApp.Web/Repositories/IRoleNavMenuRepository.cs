namespace HotelApp.Web.Repositories;

public interface IRoleNavMenuRepository
{
    Task<IReadOnlyList<int>> GetActiveMenuIdsByRoleAsync(int roleId);
    Task SaveRoleMenusAsync(int roleId, IEnumerable<int> menuIds, int? modifiedBy);
}
