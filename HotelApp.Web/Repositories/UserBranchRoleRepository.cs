using Dapper;
using Microsoft.Data.SqlClient;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public class UserBranchRoleRepository : IUserBranchRoleRepository
{
    private readonly string _connectionString;

    public UserBranchRoleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IEnumerable<UserBranchRole>> GetByUserIdAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT ubr.Id, ubr.UserId, ubr.BranchID, ubr.RoleId, ubr.IsActive,
                   b.BranchName, r.Name AS RoleName
            FROM   UserBranchRoles ubr
            INNER JOIN BranchMaster b ON b.BranchID = ubr.BranchID
            INNER JOIN Roles         r ON r.Id       = ubr.RoleId
            WHERE  ubr.UserId = @UserId AND ubr.IsActive = 1
            ORDER  BY b.BranchName, r.Name";
        return await connection.QueryAsync<UserBranchRole>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Role>> GetRolesByUserBranchAsync(int userId, int branchId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT r.Id, r.Name, r.Description, r.IconClass, r.IsSystemRole, r.BranchID, r.CreatedDate, r.LastModifiedDate
            FROM   UserBranchRoles ubr
            INNER JOIN Roles r ON r.Id = ubr.RoleId
            WHERE  ubr.UserId = @UserId AND ubr.BranchID = @BranchId AND ubr.IsActive = 1";
        return await connection.QueryAsync<Role>(sql, new { UserId = userId, BranchId = branchId });
    }

    public async Task SaveUserBranchRolesAsync(int userId, IEnumerable<UserBranchRoleAssignment> assignments, int? createdBy = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();
        try
        {
            // Soft-delete all existing active entries for this user
            await connection.ExecuteAsync(
                "UPDATE UserBranchRoles SET IsActive = 0, ModifiedDate = GETDATE(), ModifiedBy = @ModifiedBy WHERE UserId = @UserId AND IsActive = 1",
                new { UserId = userId, ModifiedBy = createdBy },
                transaction);

            // Re-insert each branch-role pair
            foreach (var assignment in assignments)
            {
                foreach (var roleId in assignment.RoleIds)
                {
                    await connection.ExecuteAsync(@"
                        IF EXISTS (SELECT 1 FROM UserBranchRoles WHERE UserId=@UserId AND BranchID=@BranchId AND RoleId=@RoleId)
                            UPDATE UserBranchRoles SET IsActive=1, ModifiedDate=GETDATE(), ModifiedBy=@CreatedBy
                            WHERE UserId=@UserId AND BranchID=@BranchId AND RoleId=@RoleId
                        ELSE
                            INSERT INTO UserBranchRoles (UserId, BranchID, RoleId, IsActive, CreatedBy, CreatedDate)
                            VALUES (@UserId, @BranchId, @RoleId, 1, @CreatedBy, GETDATE())",
                        new { UserId = userId, BranchId = assignment.BranchId, RoleId = roleId, CreatedBy = createdBy },
                        transaction);
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> HasRoleInBranchAsync(int userId, int branchId, int roleId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT COUNT(1) FROM UserBranchRoles
            WHERE UserId=@UserId AND BranchID=@BranchId AND RoleId=@RoleId AND IsActive=1";
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, BranchId = branchId, RoleId = roleId }) > 0;
    }
}
