using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IRefundRepository
{
    Task<IEnumerable<RefundListItem>> GetPendingRefundsAsync(int branchId);
    Task<IEnumerable<RefundListItem>> GetPendingRefundsByDateAsync(int branchId, DateTime fromDate, DateTime toDate);
    Task<(decimal TotalRefunded, int RefundedCount)> GetCompletedRefundsTotalAsync(int branchId, DateTime fromDate, DateTime toDate);
    Task<RefundDetailViewModel?> GetRefundDetailAsync(int cancellationId, int branchId);
    Task<ProcessRefundResult> ProcessRefundAsync(ProcessRefundRequest request, int branchId, int performedBy);
}
