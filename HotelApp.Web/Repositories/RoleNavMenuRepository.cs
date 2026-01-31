using Dapper;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

public class RoleNavMenuRepository : IRoleNavMenuRepository
{
    private readonly string _connectionString;

    public RoleNavMenuRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IReadOnlyList<int>> GetActiveMenuIdsByRoleAsync(int roleId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT NavMenuItemId
            FROM RoleNavMenuItems
            WHERE RoleId = @RoleId AND IsActive = 1";

        return (await connection.QueryAsync<int>(sql, new { RoleId = roleId })).ToList();
    }

    public async Task SaveRoleMenusAsync(int roleId, IEnumerable<int> menuIds, int? modifiedBy)
    {
        var menuIdList = menuIds.Distinct().ToList();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var tx = connection.BeginTransaction();

        try
        {
            const string deactivateSql = @"
                UPDATE RoleNavMenuItems
                SET IsActive = 0, ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
                WHERE RoleId = @RoleId";

            await connection.ExecuteAsync(deactivateSql, new { RoleId = roleId, ModifiedBy = modifiedBy }, tx);

            const string upsertSql = @"
                IF EXISTS (SELECT 1 FROM RoleNavMenuItems WHERE RoleId = @RoleId AND NavMenuItemId = @NavMenuItemId)
                BEGIN
                    UPDATE RoleNavMenuItems
                    SET IsActive = 1, ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
                    WHERE RoleId = @RoleId AND NavMenuItemId = @NavMenuItemId
                END
                ELSE
                BEGIN
                    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive, CreatedBy, CreatedDate)
                    VALUES (@RoleId, @NavMenuItemId, 1, @ModifiedBy, GETDATE())
                END";

            foreach (var menuId in menuIdList)
            {
                await connection.ExecuteAsync(upsertSql, new { RoleId = roleId, NavMenuItemId = menuId, ModifiedBy = modifiedBy }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
