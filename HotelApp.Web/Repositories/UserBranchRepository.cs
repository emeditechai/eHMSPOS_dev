using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

public class UserBranchRepository : IUserBranchRepository
{
    private readonly string _connectionString;

    public UserBranchRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<IEnumerable<UserBranch>> GetByUserIdAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT ub.*, b.*
            FROM UserBranches ub
            INNER JOIN BranchMaster b ON ub.BranchID = b.BranchID
            WHERE ub.UserId = @UserId AND ub.IsActive = 1";
        
        var userBranches = await connection.QueryAsync<UserBranch, Branch, UserBranch>(
            sql,
            (userBranch, branch) =>
            {
                userBranch.Branch = branch;
                return userBranch;
            },
            new { UserId = userId },
            splitOn: "BranchID"
        );
        
        return userBranches;
    }

    public async Task<IEnumerable<Branch>> GetBranchesByUserIdAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT b.*
            FROM UserBranches ub
            INNER JOIN BranchMaster b ON ub.BranchID = b.BranchID
            WHERE ub.UserId = @UserId AND ub.IsActive = 1 AND b.IsActive = 1";
        
        return await connection.QueryAsync<Branch>(sql, new { UserId = userId });
    }

    public async Task AssignBranchesToUserAsync(int userId, List<int> branchIds, int? createdBy = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Deactivate all existing branches for this user
            var deactivateSql = "UPDATE UserBranches SET IsActive = 0, ModifiedDate = GETDATE() WHERE UserId = @UserId";
            await connection.ExecuteAsync(deactivateSql, new { UserId = userId }, transaction);
            
            // Insert or reactivate branches
            foreach (var branchId in branchIds)
            {
                var checkSql = "SELECT Id FROM UserBranches WHERE UserId = @UserId AND BranchID = @BranchID";
                var existingId = await connection.QueryFirstOrDefaultAsync<int?>(
                    checkSql, 
                    new { UserId = userId, BranchID = branchId }, 
                    transaction
                );
                
                if (existingId.HasValue)
                {
                    // Reactivate existing record
                    var updateSql = @"
                        UPDATE UserBranches 
                        SET IsActive = 1, ModifiedDate = GETDATE(), ModifiedBy = @ModifiedBy
                        WHERE Id = @Id";
                    await connection.ExecuteAsync(
                        updateSql, 
                        new { Id = existingId.Value, ModifiedBy = createdBy }, 
                        transaction
                    );
                }
                else
                {
                    // Insert new record
                    var insertSql = @"
                        INSERT INTO UserBranches (UserId, BranchID, IsActive, CreatedBy, CreatedDate)
                        VALUES (@UserId, @BranchID, 1, @CreatedBy, GETDATE())";
                    await connection.ExecuteAsync(
                        insertSql, 
                        new { UserId = userId, BranchID = branchId, CreatedBy = createdBy }, 
                        transaction
                    );
                }
            }
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task RemoveBranchFromUserAsync(int userId, int branchId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "UPDATE UserBranches SET IsActive = 0, ModifiedDate = GETDATE() WHERE UserId = @UserId AND BranchID = @BranchID";
        await connection.ExecuteAsync(sql, new { UserId = userId, BranchID = branchId });
    }

    public async Task<bool> HasAccessToBranchAsync(int userId, int branchId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT COUNT(1) FROM UserBranches WHERE UserId = @UserId AND BranchID = @BranchID AND IsActive = 1";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, BranchID = branchId });
        return count > 0;
    }
}
