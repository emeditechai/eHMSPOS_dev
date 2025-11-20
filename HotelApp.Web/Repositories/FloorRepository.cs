using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class FloorRepository : IFloorRepository
    {
        private readonly IDbConnection _connection;

        public FloorRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<Floor>> GetAllAsync()
        {
            var sql = @"
                SELECT Id, FloorName, IsActive, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy
                FROM Floors
                ORDER BY FloorName";
            
            return await _connection.QueryAsync<Floor>(sql);
        }

        public async Task<Floor?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT Id, FloorName, IsActive, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy
                FROM Floors
                WHERE Id = @Id";
            
            return await _connection.QueryFirstOrDefaultAsync<Floor>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Floor floor)
        {
            var sql = @"
                INSERT INTO Floors (FloorName, IsActive, CreatedDate, LastModifiedDate, CreatedBy)
                VALUES (@FloorName, @IsActive, GETDATE(), GETDATE(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            return await _connection.ExecuteScalarAsync<int>(sql, new
            {
                floor.FloorName,
                floor.IsActive,
                floor.CreatedBy
            });
        }

        public async Task<bool> UpdateAsync(Floor floor)
        {
            var sql = @"
                UPDATE Floors
                SET FloorName = @FloorName,
                    IsActive = @IsActive,
                    LastModifiedDate = GETDATE(),
                    LastModifiedBy = @LastModifiedBy
                WHERE Id = @Id";
            
            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                floor.Id,
                floor.FloorName,
                floor.IsActive,
                floor.LastModifiedBy
            });
            
            return rowsAffected > 0;
        }

        // Delete removed per business rule

        public async Task<bool> FloorNameExistsAsync(string floorName, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM Floors WHERE FloorName = @FloorName AND Id != @ExcludeId"
                : "SELECT COUNT(1) FROM Floors WHERE FloorName = @FloorName";
            
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { FloorName = floorName, ExcludeId = excludeId });
            return count > 0;
        }
    }
}
