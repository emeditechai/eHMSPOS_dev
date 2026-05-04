using System.Data;
using Dapper;
using HotelApp.Web.Models;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Repositories
{
    public class B2BEInvoiceLogRepository : IB2BEInvoiceLogRepository
    {
        private readonly IDbConnection _connection;

        public B2BEInvoiceLogRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Atomically increments the global sequence counter and returns the next version
        /// in "1.X" format (e.g. "1.1", "1.2", ...).
        /// </summary>
        public async Task<string> GetNextVersionAsync()
        {
            const string sql = @"
                UPDATE dbo.EInvoiceVersionSequence WITH (HOLDLOCK)
                SET LastSequence = LastSequence + 1;
                SELECT TOP 1 LastSequence FROM dbo.EInvoiceVersionSequence;";

            var seq = await _connection.ExecuteScalarAsync<int>(sql);
            return $"1.{seq}";
        }

        public async Task<int> SaveAsync(B2BEInvoiceLog log)
        {
            const string sql = @"
                INSERT INTO dbo.B2BEInvoiceJsonLogs
                    (BookingId, BookingNo, InvoiceNumber, Version, GenerationType, JsonPayload, BranchID, CreatedDate, CreatedBy)
                VALUES
                    (@BookingId, @BookingNo, @InvoiceNumber, @Version, @GenerationType, @JsonPayload, @BranchID, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _connection.ExecuteScalarAsync<int>(sql, log);
        }

        public async Task<IEnumerable<B2BEInvoiceLog>> GetByBookingIdAsync(int bookingId)
        {
            const string sql = @"
                SELECT Id, BookingId, BookingNo, InvoiceNumber, Version, GenerationType, JsonPayload, BranchID, CreatedDate, CreatedBy
                FROM dbo.B2BEInvoiceJsonLogs
                WHERE BookingId = @BookingId
                ORDER BY CreatedDate DESC;";

            return await _connection.QueryAsync<B2BEInvoiceLog>(sql, new { BookingId = bookingId });
        }

        public async Task<bool> ExistsForBookingAsync(int bookingId)
        {
            const string sql = @"
                SELECT COUNT(1) FROM dbo.B2BEInvoiceJsonLogs WHERE BookingId = @BookingId;";

            return await _connection.ExecuteScalarAsync<int>(sql, new { BookingId = bookingId }) > 0;
        }

        public async Task<IEnumerable<B2BEInvoiceDashboardRow>> GetDashboardAsync(
            int branchId,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? generationType,
            string? bookingNoSearch,
            string? pushStatus = null)
        {
            return await _connection.QueryAsync<B2BEInvoiceDashboardRow>(
                "dbo.usp_GetB2BEInvoiceDashboard",
                new
                {
                    BranchID        = branchId,
                    FromDate        = fromDate?.ToDateTime(TimeOnly.MinValue),
                    ToDate          = toDate?.ToDateTime(TimeOnly.MinValue),
                    GenerationType  = string.IsNullOrWhiteSpace(generationType) ? null : generationType,
                    BookingNoSearch = string.IsNullOrWhiteSpace(bookingNoSearch) ? null : bookingNoSearch,
                    PushStatus      = string.IsNullOrWhiteSpace(pushStatus)      ? null : pushStatus
                },
                commandType: CommandType.StoredProcedure);
        }
    }
}
