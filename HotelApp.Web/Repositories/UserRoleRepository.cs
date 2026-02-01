using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly string _connectionString;

    public UserRoleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IEnumerable<UserRoleMapping>> GetByUserIdAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT ur.*, r.Name, r.Description, r.IconClass 
            FROM UserRoles ur
            INNER JOIN Roles r ON ur.RoleId = r.Id
            WHERE ur.UserId = @UserId AND ur.IsActive = 1
            ORDER BY r.Name";
        
        var userRoles = await connection.QueryAsync<UserRoleMapping, Role, UserRoleMapping>(
            sql,
            (userRole, role) =>
            {
                userRole.Role = role;
                return userRole;
            },
            new { UserId = userId },
            splitOn: "Name"
        );
        
        return userRoles;
    }

    public async Task<IEnumerable<Role>> GetRolesByUserIdAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT r.* 
            FROM Roles r
            INNER JOIN UserRoles ur ON r.Id = ur.RoleId
            WHERE ur.UserId = @UserId AND ur.IsActive = 1
            ORDER BY r.Name";
        
        return await connection.QueryAsync<Role>(sql, new { UserId = userId });
    }

    public async Task AssignRolesToUserAsync(int userId, IEnumerable<int> roleIds, int assignedBy)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Deactivate all existing roles for this user
            const string deactivateSql = @"
                UPDATE UserRoles 
                SET IsActive = 0, ModifiedBy = @AssignedBy, ModifiedDate = GETDATE()
                WHERE UserId = @UserId";
            await connection.ExecuteAsync(deactivateSql, new { UserId = userId, AssignedBy = assignedBy }, transaction);

            // Insert or reactivate roles
            foreach (var roleId in roleIds)
            {
                const string upsertSql = @"
                    IF EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
                    BEGIN
                        UPDATE UserRoles 
                        SET IsActive = 1, 
                            AssignedDate = GETDATE(),
                            AssignedBy = @AssignedBy,
                            ModifiedBy = @AssignedBy, 
                            ModifiedDate = GETDATE()
                        WHERE UserId = @UserId AND RoleId = @RoleId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO UserRoles (UserId, RoleId, IsActive, AssignedDate, AssignedBy, CreatedBy, CreatedDate, ModifiedDate)
                        VALUES (@UserId, @RoleId, 1, GETDATE(), @AssignedBy, @AssignedBy, GETDATE(), GETDATE())
                    END";
                
                await connection.ExecuteAsync(upsertSql, new { UserId = userId, RoleId = roleId, AssignedBy = assignedBy }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task RemoveRoleFromUserAsync(int userId, int roleId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE UserRoles 
            SET IsActive = 0, ModifiedDate = GETDATE()
            WHERE UserId = @UserId AND RoleId = @RoleId";
        
        await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
    }

    public async Task<bool> HasRoleAsync(int userId, int roleId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT COUNT(1) 
            FROM UserRoles 
            WHERE UserId = @UserId AND RoleId = @RoleId AND IsActive = 1";
        
        var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, RoleId = roleId });
        return count > 0;
    }
}
