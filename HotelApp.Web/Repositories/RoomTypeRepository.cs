using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class RoomTypeRepository : IRoomTypeRepository
    {
        private readonly IDbConnection _connection;

        public RoomTypeRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<RoomType>> GetAllAsync()
        {
            var sql = @"
                SELECT Id, TypeName, Description, BaseRate, MaxOccupancy, Max_RoomAvailability, Amenities, BranchID,
                       IsActive, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy
                FROM RoomTypes
                ORDER BY TypeName";
            
            return await _connection.QueryAsync<RoomType>(sql);
        }
        
        public async Task<IEnumerable<RoomType>> GetByBranchAsync(int branchId)
        {
            var sql = @"
                SELECT Id, TypeName, Description, BaseRate, MaxOccupancy, Max_RoomAvailability, Amenities, BranchID,
                       IsActive, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy
                FROM RoomTypes
                WHERE BranchID = @BranchId
                ORDER BY TypeName";
            
            return await _connection.QueryAsync<RoomType>(sql, new { BranchId = branchId });
        }

        public async Task<RoomType?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT Id, TypeName, Description, BaseRate, MaxOccupancy, Max_RoomAvailability, Amenities, 
                       IsActive, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy
                FROM RoomTypes
                WHERE Id = @Id";
            
            return await _connection.QueryFirstOrDefaultAsync<RoomType>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(RoomType roomType)
        {
            var sql = @"
                INSERT INTO RoomTypes (TypeName, Description, BaseRate, MaxOccupancy, Max_RoomAvailability, Amenities, BranchID,
                                       IsActive, CreatedDate, LastModifiedDate, CreatedBy)
                VALUES (@TypeName, @Description, @BaseRate, @MaxOccupancy, @Max_RoomAvailability, @Amenities, @BranchID,
                        @IsActive, GETDATE(), GETDATE(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            return await _connection.ExecuteScalarAsync<int>(sql, new
            {
                roomType.TypeName,
                roomType.Description,
                roomType.BaseRate,
                roomType.MaxOccupancy,
                roomType.Max_RoomAvailability,
                roomType.Amenities,
                roomType.BranchID,
                roomType.IsActive,
                roomType.CreatedBy
            });
        }

        public async Task<bool> UpdateAsync(RoomType roomType)
        {
            var sql = @"
                UPDATE RoomTypes
                SET TypeName = @TypeName,
                    Description = @Description,
                    BaseRate = @BaseRate,
                    MaxOccupancy = @MaxOccupancy,
                    Max_RoomAvailability = @Max_RoomAvailability,
                    Amenities = @Amenities,
                    IsActive = @IsActive,
                    LastModifiedDate = GETDATE(),
                    LastModifiedBy = @LastModifiedBy
                WHERE Id = @Id";
            
            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                roomType.Id,
                roomType.TypeName,
                roomType.Description,
                roomType.BaseRate,
                roomType.MaxOccupancy,
                roomType.Max_RoomAvailability,
                roomType.Amenities,
                roomType.IsActive,
                roomType.LastModifiedBy
            });
            
            return rowsAffected > 0;
        }

        // Delete removed per business rule

        public async Task<bool> RoomTypeNameExistsAsync(string typeName, int branchId, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM RoomTypes WHERE TypeName = @TypeName AND BranchID = @BranchId AND Id != @ExcludeId"
                : "SELECT COUNT(1) FROM RoomTypes WHERE TypeName = @TypeName AND BranchID = @BranchId";
            
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { TypeName = typeName, BranchId = branchId, ExcludeId = excludeId });
            return count > 0;
        }

        public async Task<IEnumerable<Amenity>> GetAmenitiesByBranchAsync(int branchId)
        {
            var sql = @"
                SELECT Id, AmenityName, BranchID, IsActive,
                       CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM Amenities
                WHERE BranchID = @BranchId AND IsActive = 1
                ORDER BY AmenityName";

            return await _connection.QueryAsync<Amenity>(sql, new { BranchId = branchId });
        }
    }
}
