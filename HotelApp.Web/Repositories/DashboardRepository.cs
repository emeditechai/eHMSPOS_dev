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
    }
}
