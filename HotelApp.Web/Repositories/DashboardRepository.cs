using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardStatistics?> GetDashboardStatisticsAsync(int branchId);
        Task<IEnumerable<RevenueData>> GetRevenueOverviewAsync(int branchId, int days = 7);
        Task<IEnumerable<RoomTypeDistribution>> GetRoomTypeDistributionAsync(int branchId);
        Task<IEnumerable<RecentBooking>> GetRecentBookingsAsync(int branchId, int top = 10);
        Task<RoomOccupancy> GetRoomOccupancyAsync(int branchId);
        Task<IEnumerable<BookingTrendData>> GetBookingTrendsAsync(int branchId, int days = 7);
    }

    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnection _dbConnection;

        public DashboardRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<DashboardStatistics?> GetDashboardStatisticsAsync(int branchId)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var result = await _dbConnection.QueryFirstOrDefaultAsync<DashboardStatistics>(
                "sp_GetDashboardStatistics",
                new { BranchID = branchId },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<IEnumerable<RevenueData>> GetRevenueOverviewAsync(int branchId, int days = 7)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var result = await _dbConnection.QueryAsync<RevenueData>(
                "sp_GetRevenueOverview",
                new { BranchID = branchId, Days = days },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<IEnumerable<RoomTypeDistribution>> GetRoomTypeDistributionAsync(int branchId)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var result = await _dbConnection.QueryAsync<RoomTypeDistribution>(
                "sp_GetRoomTypeDistribution",
                new { BranchID = branchId },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<IEnumerable<RecentBooking>> GetRecentBookingsAsync(int branchId, int top = 10)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var result = await _dbConnection.QueryAsync<RecentBooking>(
                "sp_GetRecentBookings",
                new { BranchID = branchId, Top = top },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<RoomOccupancy> GetRoomOccupancyAsync(int branchId)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var sql = @"
                DECLARE @Today DATE = CAST(GETDATE() AS DATE);

                SELECT
                    ISNULL(SUM(rt.Max_RoomAvailability), 0) AS TotalRooms,
                    ISNULL((
                        SELECT SUM(b.RequiredRooms)
                        FROM Bookings b
                        WHERE b.BranchID = @BranchID
                          AND b.Status IN ('Confirmed', 'CheckedIn')
                          AND CAST(b.CheckInDate AS DATE) <= @Today
                          AND (
                              CASE
                                  WHEN b.ActualCheckOutDate IS NOT NULL THEN CAST(b.ActualCheckOutDate AS DATE)
                                  ELSE CAST(b.CheckOutDate AS DATE)
                              END > @Today
                          )
                    ), 0) AS OccupiedRooms
                FROM RoomTypes rt
                WHERE rt.BranchID = @BranchID AND rt.IsActive = 1;";

            var result = await _dbConnection.QueryFirstOrDefaultAsync<RoomOccupancy>(sql, new { BranchID = branchId });
            return result ?? new RoomOccupancy();
        }

        public async Task<IEnumerable<BookingTrendData>> GetBookingTrendsAsync(int branchId, int days = 7)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var sql = @"
                DECLARE @StartDate DATE = DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS DATE));

                ;WITH DateRange AS (
                    SELECT @StartDate AS [Date]
                    UNION ALL
                    SELECT DATEADD(DAY, 1, [Date]) FROM DateRange WHERE [Date] < CAST(GETDATE() AS DATE)
                )
                SELECT
                    dr.[Date],
                    ISNULL(bc.BookingCount, 0) AS BookingCount
                FROM DateRange dr
                LEFT JOIN (
                    SELECT CAST(CreatedDate AS DATE) AS BookingDate, COUNT(*) AS BookingCount
                    FROM Bookings
                    WHERE BranchID = @BranchID
                      AND CAST(CreatedDate AS DATE) >= @StartDate
                      AND Status NOT IN ('Cancelled')
                    GROUP BY CAST(CreatedDate AS DATE)
                ) bc ON dr.[Date] = bc.BookingDate
                ORDER BY dr.[Date]
                OPTION (MAXRECURSION 365);";

            var result = await _dbConnection.QueryAsync<BookingTrendData>(sql, new { BranchID = branchId, Days = days });
            return result;
        }
    }
}
