using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HotelApp.Web.Repositories
{
    public class HotelSettingsRepository : IHotelSettingsRepository
    {
        private readonly string _connectionString;

        public HotelSettingsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' not found.");
        }

        public async Task<HotelSettings?> GetByBranchAsync(int branchId)
        {
            using var connection = new SqlConnection(_connectionString);
            
            var parameters = new DynamicParameters();
            parameters.Add("@BranchID", branchId, DbType.Int32);

            var result = await connection.QueryFirstOrDefaultAsync<HotelSettings>(
                "sp_GetHotelSettingsByBranch",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<int> UpsertAsync(HotelSettings settings, int modifiedBy)
        {
            using var connection = new SqlConnection(_connectionString);
            
            var parameters = new DynamicParameters();
            parameters.Add("@BranchID", settings.BranchID, DbType.Int32);
            parameters.Add("@HotelName", settings.HotelName, DbType.String);
            parameters.Add("@Address", settings.Address, DbType.String);
            parameters.Add("@ContactNumber1", settings.ContactNumber1, DbType.String);
            parameters.Add("@ContactNumber2", settings.ContactNumber2, DbType.String);
            parameters.Add("@EmailAddress", settings.EmailAddress, DbType.String);
            parameters.Add("@Website", settings.Website, DbType.String);
            parameters.Add("@GSTCode", settings.GSTCode, DbType.String);
            parameters.Add("@LogoPath", settings.LogoPath, DbType.String);
            parameters.Add("@CheckInTime", settings.CheckInTime, DbType.Time);
            parameters.Add("@CheckOutTime", settings.CheckOutTime, DbType.Time);
            parameters.Add("@ByPassActualDayRate", settings.ByPassActualDayRate, DbType.Boolean);
            parameters.Add("@DiscountApprovalRequired", settings.DiscountApprovalRequired, DbType.Boolean);
            parameters.Add("@MinimumBookingAmountRequired", settings.MinimumBookingAmountRequired, DbType.Boolean);
            parameters.Add("@MinimumBookingAmount", settings.MinimumBookingAmount, DbType.Decimal);
            parameters.Add("@NoShowGraceHours", settings.NoShowGraceHours, DbType.Int32);
            parameters.Add("@CancellationRefundApprovalThreshold", settings.CancellationRefundApprovalThreshold, DbType.Decimal);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            var result = await connection.ExecuteAsync(
                "sp_UpsertHotelSettings",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
    }
}
