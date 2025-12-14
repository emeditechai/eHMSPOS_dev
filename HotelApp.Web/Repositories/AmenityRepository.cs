using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class AmenityRepository : IAmenityRepository
    {
        private readonly IDbConnection _connection;

        public AmenityRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<Amenity>> GetByBranchAsync(int branchId)
        {
            var sql = @"
              SELECT Id, AmenityName, BranchID, IsActive,
                  CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM Amenities
                WHERE BranchID = @BranchId
                ORDER BY AmenityName";

            return await _connection.QueryAsync<Amenity>(sql, new { BranchId = branchId });
        }

        public async Task<Amenity?> GetByIdAsync(int id)
        {
            var sql = @"
              SELECT Id, AmenityName, BranchID, IsActive,
                  CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM Amenities
                WHERE Id = @Id";

            return await _connection.QueryFirstOrDefaultAsync<Amenity>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Amenity amenity)
        {
            var sql = @"
                INSERT INTO Amenities (AmenityName, BranchID, IsActive, CreatedDate, CreatedBy)
                VALUES (@AmenityName, @BranchID, @IsActive, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await _connection.ExecuteScalarAsync<int>(sql, new
            {
                amenity.AmenityName,
                amenity.BranchID,
                amenity.IsActive,
                amenity.CreatedBy
            });
        }

        public async Task<bool> UpdateAsync(Amenity amenity)
        {
            var sql = @"
                UPDATE Amenities
                SET AmenityName = @AmenityName,
                    IsActive = @IsActive,
                    UpdatedDate = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id";

            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                amenity.Id,
                amenity.AmenityName,
                amenity.IsActive,
                amenity.UpdatedBy
            });

            return rowsAffected > 0;
        }

        public async Task<bool> AmenityNameExistsAsync(string amenityName, int branchId, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM Amenities WHERE AmenityName = @AmenityName AND BranchID = @BranchId AND Id != @ExcludeId"
                : "SELECT COUNT(1) FROM Amenities WHERE AmenityName = @AmenityName AND BranchID = @BranchId";

            var count = await _connection.ExecuteScalarAsync<int>(sql, new { AmenityName = amenityName, BranchId = branchId, ExcludeId = excludeId });
            return count > 0;
        }
    }
}
