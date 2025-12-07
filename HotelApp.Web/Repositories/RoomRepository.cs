using System;
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
                SELECT r.Id, r.RoomNumber, r.RoomTypeId, r.Floor, r.Status, r.Notes, 
                       r.BranchID, r.IsActive, r.CreatedDate, r.LastModifiedDate,
                       f.FloorName,
                       rt.Id AS RoomType_Id, rt.TypeName, rt.Description, rt.BaseRate, rt.MaxOccupancy, rt.Amenities,
                       b.CheckInDate, b.CheckOutDate, b.BookingNumber, b.BalanceAmount, b.PrimaryGuestName
                FROM Rooms r
                INNER JOIN RoomTypes rt ON r.RoomTypeId = rt.Id
                LEFT JOIN Floors f ON r.Floor = f.Id
                LEFT JOIN (
                    SELECT br.RoomId, bk.CheckInDate, bk.CheckOutDate, bk.BookingNumber, bk.BalanceAmount,
                           CONCAT(bk.PrimaryGuestFirstName, ' ', bk.PrimaryGuestLastName) AS PrimaryGuestName,
                           ROW_NUMBER() OVER (PARTITION BY br.RoomId ORDER BY bk.CheckInDate DESC) as rn
                    FROM BookingRooms br
                    INNER JOIN Bookings bk ON br.BookingId = bk.Id
                    WHERE br.IsActive = 1
                        AND bk.Status IN ('Confirmed', 'CheckedIn')
                        AND CAST(bk.CheckInDate AS DATE) <= CAST(GETDATE() AS DATE)
                        AND CAST(bk.CheckOutDate AS DATE) > CAST(GETDATE() AS DATE)
                        AND bk.ActualCheckOutDate IS NULL
                ) b ON r.Id = b.RoomId AND b.rn = 1
                WHERE r.IsActive = 1 AND r.BranchID = @BranchId
                ORDER BY r.RoomNumber";

            var roomLookup = new Dictionary<int, Room>();
            
            await _dbConnection.QueryAsync<Room, RoomType, DateTime?, DateTime?, string, decimal?, string, Room>(
                sql,
                (room, roomType, checkInDate, checkOutDate, bookingNumber, balanceAmount, primaryGuestName) =>
                {
                    if (!roomLookup.TryGetValue(room.Id, out var existingRoom))
                    {
                        room.RoomType = roomType;
                        room.CheckInDate = checkInDate;
                        room.CheckOutDate = checkOutDate;
                        room.BookingNumber = bookingNumber;
                        room.BalanceAmount = balanceAmount;
                        room.PrimaryGuestName = primaryGuestName;
                        roomLookup.Add(room.Id, room);
                    }
                    return room;
                },
                new { BranchId = branchId },
                splitOn: "RoomType_Id,CheckInDate,CheckOutDate,BookingNumber,BalanceAmount,PrimaryGuestName"
            );

            return roomLookup.Values;
        }

        public async Task<Room?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT r.*, f.FloorName AS FloorName,
                       rt.Id AS RoomTypeId, rt.TypeName, rt.Description, rt.MaxOccupancy, rt.Amenities,
                       ISNULL((
                           SELECT TOP 1 rm.BaseRate 
                           FROM RateMaster rm 
                           WHERE rm.RoomTypeId = rt.Id 
                           AND rm.IsActive = 1 
                           AND rm.BranchID = r.BranchID
                           AND CAST(GETDATE() AS DATE) BETWEEN CAST(rm.StartDate AS DATE) AND CAST(rm.EndDate AS DATE)
                           ORDER BY rm.CreatedDate DESC
                       ), rt.BaseRate) AS BaseRate
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

        public async Task<RoomType?> GetRoomTypeByIdAsync(int id)
        {
            var sql = @"
                SELECT Id, TypeName, Description, BaseRate, MaxOccupancy, Amenities, BranchID
                FROM RoomTypes
                WHERE Id = @Id AND IsActive = 1";

            return await _dbConnection.QueryFirstOrDefaultAsync<RoomType>(sql, new { Id = id });
        }

        public async Task<bool> RoomNumberExistsAsync(string roomNumber, int branchId, int? excludeId = null)
        {
            // Check for room number existence regardless of IsActive status to match database constraint
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM Rooms WHERE RoomNumber = @RoomNumber AND BranchID = @BranchId AND Id != @ExcludeId"
                : "SELECT COUNT(1) FROM Rooms WHERE RoomNumber = @RoomNumber AND BranchID = @BranchId";

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

        public async Task<Dictionary<string, int>> GetRoomStatusCountsAsync(int branchId)
        {
            var sql = @"
                SELECT Status, COUNT(*) as Count
                FROM Rooms
                WHERE IsActive = 1 AND BranchID = @BranchId
                GROUP BY Status";

            var results = await _dbConnection.QueryAsync<(string Status, int Count)>(sql, new { BranchId = branchId });
            
            var statusCounts = new Dictionary<string, int>
            {
                { "Available", 0 },
                { "Occupied", 0 },
                { "Maintenance", 0 },
                { "Cleaning", 0 }
            };

            foreach (var (status, count) in results)
            {
                if (statusCounts.ContainsKey(status))
                {
                    statusCounts[status] = count;
                }
            }

            return statusCounts;
        }

        public async Task<Dictionary<string, int>> GetYesterdayRoomStatusCountsAsync(int branchId)
        {
            // For now, return current counts. In production, you'd track historical status changes
            // or use a StatusHistory table to get yesterday's counts
            return await GetRoomStatusCountsAsync(branchId);
        }

        public async Task<bool> UpdateRoomStatusAsync(int roomId, string status, int modifiedBy)
        {
            var sql = @"
                UPDATE Rooms
                SET Status = @Status,
                    LastModifiedDate = GETUTCDATE()
                WHERE Id = @RoomId AND IsActive = 1";

            var rowsAffected = await _dbConnection.ExecuteAsync(sql, new { RoomId = roomId, Status = status });
            return rowsAffected > 0;
        }

        public async Task<(bool hasActiveBooking, string? bookingNumber, decimal balanceAmount)> GetActiveBookingForRoomAsync(int roomId)
        {
            var sql = @"
                SELECT TOP 1 
                    BookingNumber,
                    BalanceAmount,
                    PaymentStatus
                FROM Bookings
                WHERE RoomId = @RoomId 
                    AND Status IN ('Confirmed', 'CheckedIn')
                    AND CAST(GETDATE() AS DATE) BETWEEN CAST(CheckInDate AS DATE) AND CAST(CheckOutDate AS DATE)
                ORDER BY CheckInDate DESC";

            var booking = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(sql, new { RoomId = roomId });

            if (booking == null)
            {
                return (false, null, 0);
            }

            var bookingNumber = booking.BookingNumber as string;
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return (false, null, 0);
            }

            decimal balanceAmount = booking.BalanceAmount is decimal dec ? dec : Convert.ToDecimal(booking.BalanceAmount ?? 0m);
            return (true, bookingNumber, balanceAmount);
        }

        public async Task<(bool hasBooking, string? bookingNumber, decimal balanceAmount, DateTime? checkOutDate)> GetAnyBookingForRoomAsync(int roomId)
        {
            var sql = @"
                SELECT TOP 1 
                    BookingNumber,
                    BalanceAmount,
                    CheckOutDate,
                    PaymentStatus
                FROM Bookings
                WHERE RoomId = @RoomId 
                    AND Status IN ('Confirmed', 'CheckedIn')
                ORDER BY CheckInDate DESC";

            var booking = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(sql, new { RoomId = roomId });

            if (booking == null)
            {
                return (false, null, 0, null);
            }

            var bookingNumber = booking.BookingNumber as string;
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return (false, null, 0, null);
            }

            decimal balanceAmount = booking.BalanceAmount is decimal dec ? dec : Convert.ToDecimal(booking.BalanceAmount ?? 0m);
            DateTime? checkOutDate = booking.CheckOutDate is DateTime dt
                ? dt
                : booking.CheckOutDate == null ? null : Convert.ToDateTime(booking.CheckOutDate);
            return (true, bookingNumber, balanceAmount, checkOutDate);
        }

        public async Task<Dictionary<int, (string roomTypeName, int totalRooms, int availableRooms, decimal baseRate, int maxOccupancy, List<string> availableRoomNumbers)>> GetRoomAvailabilityByDateRangeAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var sql = @"
                -- Count required rooms by room type in the date range (includes bookings with or without room assignments)
                WITH BookingsByRoomType AS (
                    SELECT 
                        b.RoomTypeId,
                        SUM(b.RequiredRooms) AS TotalRequiredRooms
                    FROM Bookings b
                    WHERE b.BranchID = @BranchId
                        AND b.Status IN ('Confirmed', 'CheckedIn')
                        -- Room is occupied if check-in is before or on the end date
                        -- AND actual checkout hasn't happened yet OR planned checkout is after start date
                        AND CAST(b.CheckInDate AS DATE) <= CAST(@EndDate AS DATE)
                        AND (
                            -- If ActualCheckOutDate is set, use it; otherwise use CheckOutDate
                            CASE 
                                WHEN b.ActualCheckOutDate IS NOT NULL 
                                THEN CAST(b.ActualCheckOutDate AS DATE)
                                ELSE CAST(b.CheckOutDate AS DATE)
                            END > CAST(@StartDate AS DATE)
                        )
                    GROUP BY b.RoomTypeId
                ),
                -- Get all active rooms by room type that are available (not already assigned to bookings in this range)
                AvailableRoomsForRange AS (
                    SELECT 
                        r.RoomTypeId,
                        r.RoomNumber
                    FROM Rooms r
                    WHERE r.BranchID = @BranchId
                        AND r.IsActive = 1
                        AND r.Status IN ('Available', 'Occupied')
                        AND NOT EXISTS (
                            -- Exclude rooms that have bookings assigned via BookingRooms table in the date range
                            -- Room is occupied if actual checkout hasn't happened yet OR planned checkout is after start date
                            SELECT 1 
                            FROM BookingRooms br
                            INNER JOIN Bookings b2 ON br.BookingId = b2.Id
                            WHERE br.RoomId = r.Id
                                AND br.IsActive = 1
                                AND b2.Status IN ('Confirmed', 'CheckedIn')
                                AND CAST(b2.CheckInDate AS DATE) <= CAST(@EndDate AS DATE)
                                AND (
                                    -- If ActualCheckOutDate is set, use it; otherwise use CheckOutDate
                                    CASE 
                                        WHEN b2.ActualCheckOutDate IS NOT NULL 
                                        THEN CAST(b2.ActualCheckOutDate AS DATE)
                                        ELSE CAST(b2.CheckOutDate AS DATE)
                                    END > CAST(@StartDate AS DATE)
                                )
                        )
                )
                SELECT 
                    rt.Id AS RoomTypeId,
                    rt.TypeName AS RoomTypeName,
                    rt.MaxOccupancy,
                    rt.Max_RoomAvailability AS MaxRoomAvailability,
                    -- Get base rate from RateMaster for current date range, fallback to RoomType.BaseRate
                    ISNULL(
                        (SELECT TOP 1 rm.BaseRate 
                         FROM RateMaster rm 
                         WHERE rm.RoomTypeId = rt.Id 
                         AND rm.IsActive = 1 
                         AND rm.BranchID = @BranchId
                         AND CAST(@StartDate AS DATE) BETWEEN CAST(rm.StartDate AS DATE) AND CAST(rm.EndDate AS DATE)
                         ORDER BY 
                            CASE WHEN rm.CustomerType = 'B2C' THEN 1 ELSE 2 END,
                            rm.CreatedDate DESC
                        ), 
                        rt.BaseRate
                    ) AS BaseRate,
                    -- Get discount if any
                    ISNULL(
                        (SELECT TOP 1 rm.ApplyDiscount 
                         FROM RateMaster rm 
                         WHERE rm.RoomTypeId = rt.Id 
                         AND rm.IsActive = 1 
                         AND rm.BranchID = @BranchId
                         AND CAST(@StartDate AS DATE) BETWEEN CAST(rm.StartDate AS DATE) AND CAST(rm.EndDate AS DATE)
                         ORDER BY 
                            CASE WHEN rm.CustomerType = 'B2C' THEN 1 ELSE 2 END,
                            rm.CreatedDate DESC
                        ), 
                        NULL
                    ) AS ApplyDiscount,
                    -- Get the total required rooms in the date range for this room type
                    ISNULL(brt.TotalRequiredRooms, 0) AS TotalRequiredRooms,
                    -- Get comma-separated list of available room numbers (rooms without assignments)
                    STRING_AGG(ar.RoomNumber, ', ') AS AvailableRoomNumbers
                FROM RoomTypes rt
                LEFT JOIN BookingsByRoomType brt ON rt.Id = brt.RoomTypeId
                LEFT JOIN AvailableRoomsForRange ar ON rt.Id = ar.RoomTypeId
                WHERE rt.BranchID = @BranchId
                    AND rt.IsActive = 1
                GROUP BY rt.Id, rt.TypeName, rt.MaxOccupancy, rt.Max_RoomAvailability, brt.TotalRequiredRooms
                ORDER BY rt.TypeName";

            var results = await _dbConnection.QueryAsync(sql, new { BranchId = branchId, StartDate = startDate, EndDate = endDate });
            
            var availability = new Dictionary<int, (string, int, int, decimal, int, List<string>)>();
            
            foreach (var row in results)
            {
                int roomTypeId = row.RoomTypeId;
                string roomTypeName = row.RoomTypeName ?? "";
                decimal baseRate = row.BaseRate ?? 0m;
                int maxOccupancy = row.MaxOccupancy ?? 0;
                
                // Get Max_RoomAvailability from RoomTypes table (configured max capacity for this room type)
                int? maxRoomAvailability = row.MaxRoomAvailability;
                
                // Get the total required rooms in the date range (sum of RequiredRooms from all bookings)
                int totalRequiredRooms = row.TotalRequiredRooms ?? 0;
                
                // Use Max_RoomAvailability as the total capacity for this room type
                // This represents the maximum number of rooms of this type that can be booked
                int totalCapacity = maxRoomAvailability ?? 0;
                
                // Calculate available rooms: Total Capacity - Total Required Rooms in Range
                int availableRooms = Math.Max(0, totalCapacity - totalRequiredRooms);
                
                // Parse available room numbers (these are rooms that don't have bookings in the range)
                string availableRoomNumbersStr = row.AvailableRoomNumbers as string ?? "";
                var roomNumbers = string.IsNullOrWhiteSpace(availableRoomNumbersStr) 
                    ? new List<string>() 
                    : availableRoomNumbersStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Take(availableRooms)  // Only take up to available count
                        .ToList();
                
                // Return: totalCapacity as total rooms, calculated available rooms
                availability[roomTypeId] = (roomTypeName, totalCapacity, availableRooms, baseRate, maxOccupancy, roomNumbers);
            }
            
            return availability;
        }
    }
}
