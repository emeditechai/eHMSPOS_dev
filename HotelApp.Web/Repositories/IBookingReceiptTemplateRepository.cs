using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IBookingReceiptTemplateRepository
    {
        Task<BookingReceiptTemplateSettings?> GetByBranchAsync(int branchId);
        Task<int> UpsertAsync(int branchId, string templateKey, int? modifiedBy);
    }
}
