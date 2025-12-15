using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class OtherChargeRepository : IOtherChargeRepository
    {
        private readonly IDbConnection _connection;

        public OtherChargeRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<OtherCharge>> GetByBranchAsync(int branchId)
        {
            var sql = @"
              SELECT Id, Code, [Name], [Type], Rate,
                     GSTPercent, CGSTPercent, SGSTPercent,
                     BranchID, IsActive,
                     CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM OtherCharges
               WHERE BranchID = @BranchId
               ORDER BY [Name], Code";

            return await _connection.QueryAsync<OtherCharge>(sql, new { BranchId = branchId });
        }

        public async Task<OtherCharge?> GetByIdAsync(int id)
        {
            var sql = @"
              SELECT Id, Code, [Name], [Type], Rate,
                     GSTPercent, CGSTPercent, SGSTPercent,
                     BranchID, IsActive,
                     CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM OtherCharges
               WHERE Id = @Id";

            return await _connection.QueryFirstOrDefaultAsync<OtherCharge>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(OtherCharge otherCharge)
        {
            var sql = @"
                INSERT INTO OtherCharges
                    (Code, [Name], [Type], Rate, GSTPercent, CGSTPercent, SGSTPercent, BranchID, IsActive, CreatedDate, CreatedBy)
                VALUES
                    (@Code, @Name, @Type, @Rate, @GSTPercent, @CGSTPercent, @SGSTPercent, @BranchID, @IsActive, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await _connection.ExecuteScalarAsync<int>(sql, new
            {
                otherCharge.Code,
                otherCharge.Name,
                Type = (int)otherCharge.Type,
                otherCharge.Rate,
                otherCharge.GSTPercent,
                otherCharge.CGSTPercent,
                otherCharge.SGSTPercent,
                otherCharge.BranchID,
                otherCharge.IsActive,
                otherCharge.CreatedBy
            });
        }

        public async Task<bool> UpdateAsync(OtherCharge otherCharge)
        {
            var sql = @"
                UPDATE OtherCharges
                   SET Code = @Code,
                       [Name] = @Name,
                       [Type] = @Type,
                       Rate = @Rate,
                       GSTPercent = @GSTPercent,
                       CGSTPercent = @CGSTPercent,
                       SGSTPercent = @SGSTPercent,
                       IsActive = @IsActive,
                       UpdatedDate = SYSUTCDATETIME(),
                       UpdatedBy = @UpdatedBy
                 WHERE Id = @Id";

            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                otherCharge.Id,
                otherCharge.Code,
                otherCharge.Name,
                Type = (int)otherCharge.Type,
                otherCharge.Rate,
                otherCharge.GSTPercent,
                otherCharge.CGSTPercent,
                otherCharge.SGSTPercent,
                otherCharge.IsActive,
                otherCharge.UpdatedBy
            });

            return rowsAffected > 0;
        }

        public async Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM OtherCharges WHERE Code = @Code AND BranchID = @BranchId AND Id != @ExcludeId"
                : "SELECT COUNT(1) FROM OtherCharges WHERE Code = @Code AND BranchID = @BranchId";

            var count = await _connection.ExecuteScalarAsync<int>(sql, new
            {
                Code = code,
                BranchId = branchId,
                ExcludeId = excludeId
            });

            return count > 0;
        }
    }
}
