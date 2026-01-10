using System.Data;
using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories
{
    public class BookingReceiptTemplateRepository : IBookingReceiptTemplateRepository
    {
        private readonly string _connectionString;

        public BookingReceiptTemplateRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' not found.");
        }

        public async Task<BookingReceiptTemplateSettings?> GetByBranchAsync(int branchId)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@BranchID", branchId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<BookingReceiptTemplateSettings>(
                "sp_GetBookingReceiptTemplateByBranch",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertAsync(int branchId, string templateKey, int? modifiedBy)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@BranchID", branchId, DbType.Int32);
            parameters.Add("@TemplateKey", templateKey, DbType.String);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            // Stored proc returns Id
            return await connection.ExecuteScalarAsync<int>(
                "sp_UpsertBookingReceiptTemplateByBranch",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
