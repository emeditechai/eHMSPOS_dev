using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IB2BEInvoiceLogRepository
    {
        /// <summary>
        /// Returns the next unique version number in "1.X" format using a DB sequence.
        /// </summary>
        Task<string> GetNextVersionAsync();

        /// <summary>
        /// Persists a generated e-invoice JSON log row.
        /// </summary>
        Task<int> SaveAsync(B2BEInvoiceLog log);

        /// <summary>
        /// Returns all e-invoice logs for a given booking.
        /// </summary>
        Task<IEnumerable<B2BEInvoiceLog>> GetByBookingIdAsync(int bookingId);

        /// <summary>
        /// Returns whether a log already exists for the given booking (to avoid duplicates).
        /// </summary>
        Task<bool> ExistsForBookingAsync(int bookingId);

        /// <summary>
        /// Fetch a single log row by primary key.
        /// </summary>
        Task<B2BEInvoiceLog?> GetByIdAsync(int id);

        /// <summary>
        /// Persists the IRP response (IRN, QR code, push status) after a portal submission attempt.
        /// </summary>
        Task UpdateIrnResponseAsync(
            int logId,
            string? irn,
            string? ackNo,
            string? ackDt,
            string? signedQRCode,
            string pushStatus,          // PUSHED | FAILED
            string? irnRequestJson,
            string? irnResponseJson);

        /// <summary>
        /// Returns dashboard rows via stored procedure with filters.
        /// </summary>
        Task<IEnumerable<HotelApp.Web.ViewModels.B2BEInvoiceDashboardRow>> GetDashboardAsync(
            int branchId,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? generationType,
            string? bookingNoSearch,
            string? pushStatus = null);
    }
}
