using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface ICreditNoteRepository
{
    /// <summary>
    /// Generates and persists a credit note for a processed refund.
    /// Returns the new credit note Id and number, or (0, null) on failure.
    /// </summary>
    Task<(int CreditNoteId, string? CreditNoteNumber)> CreateAsync(
        int cancellationId,
        int? refundPaymentId,
        string? refundPaymentMethod,
        string? refundPaymentReference,
        int branchId,
        int performedBy);

    Task<CreditNote?> GetByIdAsync(int id);
    Task<CreditNote?> GetByCancellationIdAsync(int cancellationId);
    Task<IEnumerable<CreditNote>> GetByDateRangeAsync(int branchId, DateTime from, DateTime to);
}
