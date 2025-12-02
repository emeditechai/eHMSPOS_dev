using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly string _connectionString;

        public BranchRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string not found");
        }

        public async Task<IEnumerable<Branch>> GetAllBranchesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT BranchID, BranchName, BranchCode, Country, State, City, 
                       Address, Pincode, IsHOBranch, IsActive, CreatedBy, CreatedDate, 
                       ModifiedBy, ModifiedDate
                FROM BranchMaster
                ORDER BY BranchName";
            
            return await connection.QueryAsync<Branch>(sql);
        }

        public async Task<IEnumerable<Branch>> GetActiveBranchesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT BranchID, BranchName, BranchCode, Country, State, City, 
                       Address, Pincode, IsHOBranch, IsActive, CreatedBy, CreatedDate, 
                       ModifiedBy, ModifiedDate
                FROM BranchMaster
                WHERE IsActive = 1
                ORDER BY BranchName";
            
            return await connection.QueryAsync<Branch>(sql);
        }

        public async Task<Branch?> GetBranchByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT BranchID, BranchName, BranchCode, Country, State, City, 
                       Address, Pincode, IsHOBranch, IsActive, CreatedBy, CreatedDate, 
                       ModifiedBy, ModifiedDate
                FROM BranchMaster
                WHERE BranchID = @Id";
            
            return await connection.QueryFirstOrDefaultAsync<Branch>(sql, new { Id = id });
        }

        public async Task<Branch?> GetBranchByCodeAsync(string branchCode)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT BranchID, BranchName, BranchCode, Country, State, City, 
                       Address, Pincode, IsHOBranch, IsActive, CreatedBy, CreatedDate, 
                       ModifiedBy, ModifiedDate
                FROM BranchMaster
                WHERE BranchCode = @BranchCode";
            
            return await connection.QueryFirstOrDefaultAsync<Branch>(sql, new { BranchCode = branchCode });
        }

        public async Task<Branch?> GetHOBranchAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT BranchID, BranchName, BranchCode, Country, State, City, 
                       Address, Pincode, IsHOBranch, IsActive, CreatedBy, CreatedDate, 
                       ModifiedBy, ModifiedDate
                FROM BranchMaster
                WHERE IsHOBranch = 1 AND IsActive = 1";
            
            return await connection.QueryFirstOrDefaultAsync<Branch>(sql);
        }

        public async Task<int> CreateBranchAsync(Branch branch)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                INSERT INTO BranchMaster 
                (BranchName, BranchCode, Country, State, City, Address, Pincode, 
                 IsHOBranch, IsActive, CreatedBy, CreatedDate)
                VALUES 
                (@BranchName, @BranchCode, @Country, @State, @City, @Address, @Pincode, 
                 @IsHOBranch, @IsActive, @CreatedBy, @CreatedDate);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            return await connection.ExecuteScalarAsync<int>(sql, branch);
        }

        public async Task<bool> UpdateBranchAsync(Branch branch)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                UPDATE BranchMaster
                SET BranchName = @BranchName,
                    BranchCode = @BranchCode,
                    Country = @Country,
                    State = @State,
                    City = @City,
                    Address = @Address,
                    Pincode = @Pincode,
                    IsHOBranch = @IsHOBranch,
                    IsActive = @IsActive,
                    ModifiedBy = @ModifiedBy,
                    ModifiedDate = @ModifiedDate
                WHERE BranchID = @BranchID";
            
            var rowsAffected = await connection.ExecuteAsync(sql, branch);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE BranchMaster SET IsActive = 0 WHERE BranchID = @Id";
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> BranchCodeExistsAsync(string branchCode, int? excludeId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT COUNT(1)
                FROM BranchMaster
                WHERE BranchCode = @BranchCode
                AND (@ExcludeId IS NULL OR BranchID != @ExcludeId)";
            
            var count = await connection.ExecuteScalarAsync<int>(sql, new { BranchCode = branchCode, ExcludeId = excludeId });
            return count > 0;
        }

        public async Task<bool> BranchNameExistsAsync(string branchName, int? excludeId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT COUNT(1)
                FROM BranchMaster
                WHERE BranchName = @BranchName
                AND (@ExcludeId IS NULL OR BranchID != @ExcludeId)";
            
            var count = await connection.ExecuteScalarAsync<int>(sql, new { BranchName = branchName, ExcludeId = excludeId });
            return count > 0;
        }
    }
}
