using Dapper;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

public class AuthorizationPermissionRepository : IAuthorizationPermissionRepository
{
    private readonly string _connectionString;

    public AuthorizationPermissionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IReadOnlyList<(int ResourceId, bool IsAllowed)>> GetRolePermissionsAsync(int roleId, int? branchId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT ResourceId, IsAllowed
            FROM AuthorizationRolePermissions
            WHERE RoleId = @RoleId AND IsActive = 1 AND ((@BranchId IS NULL AND BranchID IS NULL) OR BranchID = @BranchId)";

        var rows = await connection.QueryAsync(sql, new { RoleId = roleId, BranchId = branchId });
        return rows.Select(r => ((int)r.ResourceId, (bool)r.IsAllowed)).ToList();
    }

    public async Task<IReadOnlyList<(int ResourceId, bool IsAllowed)>> GetUserPermissionsAsync(int userId, int? branchId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT ResourceId, IsAllowed
            FROM AuthorizationUserPermissions
            WHERE UserId = @UserId AND IsActive = 1 AND ((@BranchId IS NULL AND BranchID IS NULL) OR BranchID = @BranchId)";

        var rows = await connection.QueryAsync(sql, new { UserId = userId, BranchId = branchId });
        return rows.Select(r => ((int)r.ResourceId, (bool)r.IsAllowed)).ToList();
    }

    public async Task UpsertRolePermissionsAsync(int roleId, int? branchId, IEnumerable<(int ResourceId, bool IsAllowed)> permissions, int? modifiedBy)
    {
        var list = permissions.DistinctBy(x => x.ResourceId).ToList();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var tx = connection.BeginTransaction();

        try
        {
            const string deactivateSql = @"
                UPDATE AuthorizationRolePermissions
                SET IsActive = 0, ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
                WHERE RoleId = @RoleId AND ((@BranchId IS NULL AND BranchID IS NULL) OR BranchID = @BranchId)";

            await connection.ExecuteAsync(deactivateSql, new { RoleId = roleId, BranchId = branchId, ModifiedBy = modifiedBy }, tx);

            const string upsertSql = @"
                IF EXISTS (SELECT 1 FROM AuthorizationRolePermissions WHERE RoleId = @RoleId AND ResourceId = @ResourceId AND BranchIdNormalized = ISNULL(@BranchId, 0))
                BEGIN
                    UPDATE AuthorizationRolePermissions
                    SET IsActive = 1, IsAllowed = @IsAllowed, ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
                    WHERE RoleId = @RoleId AND ResourceId = @ResourceId AND BranchIdNormalized = ISNULL(@BranchId, 0)
                END
                ELSE
                BEGIN
                    INSERT INTO AuthorizationRolePermissions (RoleId, ResourceId, BranchID, IsAllowed, IsActive, CreatedBy, CreatedDate)
                    VALUES (@RoleId, @ResourceId, @BranchId, @IsAllowed, 1, @ModifiedBy, GETDATE())
                END";

            foreach (var (resourceId, isAllowed) in list)
            {
                await connection.ExecuteAsync(upsertSql, new
                {
                    RoleId = roleId,
                    ResourceId = resourceId,
                    BranchId = branchId,
                    IsAllowed = isAllowed,
                    ModifiedBy = modifiedBy
                }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpsertUserPermissionsAsync(int userId, int? branchId, IEnumerable<(int ResourceId, bool IsAllowed)> permissions, int? modifiedBy)
    {
        var list = permissions.DistinctBy(x => x.ResourceId).ToList();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var tx = connection.BeginTransaction();

        try
        {
            const string deactivateSql = @"
                UPDATE AuthorizationUserPermissions
                SET IsActive = 0, ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
                WHERE UserId = @UserId AND ((@BranchId IS NULL AND BranchID IS NULL) OR BranchID = @BranchId)";

            await connection.ExecuteAsync(deactivateSql, new { UserId = userId, BranchId = branchId, ModifiedBy = modifiedBy }, tx);

            const string upsertSql = @"
                IF EXISTS (SELECT 1 FROM AuthorizationUserPermissions WHERE UserId = @UserId AND ResourceId = @ResourceId AND BranchIdNormalized = ISNULL(@BranchId, 0))
                BEGIN
                    UPDATE AuthorizationUserPermissions
                    SET IsActive = 1, IsAllowed = @IsAllowed, ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
                    WHERE UserId = @UserId AND ResourceId = @ResourceId AND BranchIdNormalized = ISNULL(@BranchId, 0)
                END
                ELSE
                BEGIN
                    INSERT INTO AuthorizationUserPermissions (UserId, ResourceId, BranchID, IsAllowed, IsActive, CreatedBy, CreatedDate)
                    VALUES (@UserId, @ResourceId, @BranchId, @IsAllowed, 1, @ModifiedBy, GETDATE())
                END";

            foreach (var (resourceId, isAllowed) in list)
            {
                await connection.ExecuteAsync(upsertSql, new
                {
                    UserId = userId,
                    ResourceId = resourceId,
                    BranchId = branchId,
                    IsAllowed = isAllowed,
                    ModifiedBy = modifiedBy
                }, tx);
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
