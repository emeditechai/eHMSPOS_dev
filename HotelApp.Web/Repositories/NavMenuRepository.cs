using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

public class NavMenuRepository : INavMenuRepository
{
    private readonly string _connectionString;

    public NavMenuRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IReadOnlyList<NavMenuItem>> GetAllActiveAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive
            FROM NavMenuItems
            WHERE IsActive = 1
            ORDER BY SortOrder, Title";

        return (await connection.QueryAsync<NavMenuItem>(sql)).ToList();
    }

    public async Task<IReadOnlyList<NavMenuItem>> GetActiveForUserAsync(int userId, bool isAdmin)
    {
        var allActive = (await GetAllActiveAsync()).ToList();
        if (isAdmin)
        {
            return allActive;
        }

        using var connection = new SqlConnection(_connectionString);
        const string allowedSql = @"
            SELECT DISTINCT mi.Id
            FROM NavMenuItems mi
            INNER JOIN RoleNavMenuItems rmi ON rmi.NavMenuItemId = mi.Id AND rmi.IsActive = 1
            INNER JOIN UserRoles ur ON ur.RoleId = rmi.RoleId AND ur.UserId = @UserId AND ur.IsActive = 1
            WHERE mi.IsActive = 1";

        var allowedIds = (await connection.QueryAsync<int>(allowedSql, new { UserId = userId })).ToHashSet();
        if (allowedIds.Count == 0)
        {
            return new List<NavMenuItem>();
        }

        var byId = allActive.ToDictionary(x => x.Id, x => x);
        var included = new HashSet<int>();

        foreach (var id in allowedIds)
        {
            if (!byId.TryGetValue(id, out var item))
            {
                continue;
            }

            // Include this item and all ancestors.
            var current = item;
            while (true)
            {
                if (!included.Add(current.Id))
                {
                    break;
                }

                if (current.ParentId is null)
                {
                    break;
                }

                if (!byId.TryGetValue(current.ParentId.Value, out var parent))
                {
                    break;
                }

                current = parent;
            }
        }

        return allActive.Where(x => included.Contains(x.Id)).OrderBy(x => x.SortOrder).ThenBy(x => x.Title).ToList();
    }
}
