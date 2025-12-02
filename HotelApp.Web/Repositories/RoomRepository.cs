using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly IDbConnection _dbConnection;

        public RoomRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Room>> GetAllAsync()
        {
            var sql = @"
                SELECT r.*, f.FloorName AS FloorName,
                       rt.Id AS RoomTypeId, rt.TypeName, rt.Description, rt.BaseRate, rt.MaxOccupancy, rt.Amenities
                FROM Rooms r
                INNER JOIN RoomTypes rt ON r.RoomTypeId = rt.Id
                LEFT JOIN Floors f ON r.Floor = f.Id
                WHERE r.IsActive = 1
                ORDER BY r.RoomNumber";

            var rooms = await _dbConnection.QueryAsync<Room, RoomType, Room>(
                sql,
                (room, roomType) =>
                {
                    room.RoomType = roomType;
                    return room;
                },
                splitOn: "TypeName"
            );

            return rooms;
        }

        public async Task<IEnumerable<Room>> GetAllByBranchAsync(int branchId)
        {
            var sql = @"
                SELECT r.*, f.FloorName AS FloorName,
                       rt.Id AS RoomTypeId, rt.TypeName, rt.Description, rt.BaseRate, rt.MaxOccupancy, rt.Amenities
                FROM Rooms r
                INNER JOIN RoomTypes rt ON r.RoomTypeId = rt.Id
                LEFT JOIN Floors f ON r.Floor = f.Id
                WHERE r.IsActive = 1 AND r.BranchID = @BranchId
                ORDER BY r.RoomNumber";

            var rooms = await _dbConnection.QueryAsync<Room, RoomType, Room>(
                sql,
                (room, roomType) =>
                {
                    room.RoomType = roomType;
                    return room;
                },
                new { BranchId = branchId },
                splitOn: "TypeName"
            );

            return rooms;
        }

        public async Task<Room?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT r.*, f.FloorName AS FloorName,
                       rt.Id AS RoomTypeId, rt.TypeName, rt.Description, rt.BaseRate, rt.MaxOccupancy, rt.Amenities
                FROM Rooms r
                INNER JOIN RoomTypes rt ON r.RoomTypeId = rt.Id
                LEFT JOIN Floors f ON r.Floor = f.Id
                WHERE r.Id = @Id AND r.IsActive = 1";

            var rooms = await _dbConnection.QueryAsync<Room, RoomType, Room>(
                sql,
                (room, roomType) =>
                {
                    room.RoomType = roomType;
                    return room;
                },
                new { Id = id },
                splitOn: "TypeName"
            );

            return rooms.FirstOrDefault();
        }

        public async Task<Room?> GetByRoomNumberAsync(string roomNumber)
        {
            var sql = @"
                SELECT r.*, f.FloorName AS FloorName,
                       rt.Id AS RoomTypeId, rt.TypeName, rt.Description, rt.BaseRate, rt.MaxOccupancy, rt.Amenities
                FROM Rooms r
                INNER JOIN RoomTypes rt ON r.RoomTypeId = rt.Id
                LEFT JOIN Floors f ON r.Floor = f.Id
                WHERE r.RoomNumber = @RoomNumber AND r.IsActive = 1";

            var rooms = await _dbConnection.QueryAsync<Room, RoomType, Room>(
                sql,
                (room, roomType) =>
                {
                    room.RoomType = roomType;
                    return room;
                },
                new { RoomNumber = roomNumber },
                splitOn: "TypeName"
            );

            return rooms.FirstOrDefault();
        }

        public async Task<int> CreateAsync(Room room)
        {
            var sql = @"
                INSERT INTO Rooms (RoomNumber, RoomTypeId, Floor, Status, Notes, BranchID, IsActive, CreatedDate, LastModifiedDate)
                VALUES (@RoomNumber, @RoomTypeId, @Floor, @Status, @Notes, @BranchID, @IsActive, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, room);
            return id;
        }

        public async Task<bool> UpdateAsync(Room room)
        {
            var sql = @"
                UPDATE Rooms
                SET RoomNumber = @RoomNumber,
                    RoomTypeId = @RoomTypeId,
                    Floor = @Floor,
                    Status = @Status,
                    Notes = @Notes,
                    IsActive = @IsActive,
                    LastModifiedDate = GETDATE()
                WHERE Id = @Id";

            var affectedRows = await _dbConnection.ExecuteAsync(sql, room);
            return affectedRows > 0;
        }

        // Delete removed per business rule

        public async Task<IEnumerable<RoomType>> GetRoomTypesAsync()
        {
            var sql = @"
                SELECT Id, TypeName, Description, BaseRate, MaxOccupancy, Amenities, BranchID
                FROM RoomTypes
                WHERE IsActive = 1
                ORDER BY TypeName";

            return await _dbConnection.QueryAsync<RoomType>(sql);
        }
        
        public async Task<IEnumerable<RoomType>> GetRoomTypesByBranchAsync(int branchId)
        {
            var sql = @"
                SELECT Id, TypeName, Description, BaseRate, MaxOccupancy, Amenities, BranchID
                FROM RoomTypes
                WHERE IsActive = 1 AND BranchID = @BranchId
                ORDER BY TypeName";

            return await _dbConnection.QueryAsync<RoomType>(sql, new { BranchId = branchId });
        }

        public async Task<bool> RoomNumberExistsAsync(string roomNumber, int branchId, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM Rooms WHERE RoomNumber = @RoomNumber AND BranchID = @BranchId AND Id != @ExcludeId AND IsActive = 1"
                : "SELECT COUNT(1) FROM Rooms WHERE RoomNumber = @RoomNumber AND BranchID = @BranchId AND IsActive = 1";

            var count = await _dbConnection.ExecuteScalarAsync<int>(sql, new { RoomNumber = roomNumber, BranchId = branchId, ExcludeId = excludeId });
            return count > 0;
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, string? excludeBookingNumber = null)
        {
            // Check if room has any conflicting bookings
            var sql = @"
                SELECT COUNT(1)
                FROM BookingRoomNights brn
                INNER JOIN Bookings b ON brn.BookingId = b.Id
                WHERE brn.RoomId = @RoomId
                    AND brn.StayDate >= @CheckInDate
                    AND brn.StayDate < @CheckOutDate
                    AND b.Status NOT IN ('Cancelled', 'No-Show')
                    AND (@ExcludeBookingNumber IS NULL OR b.BookingNumber != @ExcludeBookingNumber)";

            var conflictCount = await _dbConnection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    RoomId = roomId,
                    CheckInDate = checkInDate.Date,
                    CheckOutDate = checkOutDate.Date,
                    ExcludeBookingNumber = excludeBookingNumber
                }
            );

            return conflictCount == 0;
        }
    }
}
