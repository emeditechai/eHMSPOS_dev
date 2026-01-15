using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IGuestFeedbackRepository
    {
        Task<int> CreateAsync(GuestFeedback feedback);
        Task<GuestFeedback?> GetByIdAsync(int id, int branchId);
        Task<IEnumerable<GuestFeedback>> GetByBranchAsync(int branchId, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
