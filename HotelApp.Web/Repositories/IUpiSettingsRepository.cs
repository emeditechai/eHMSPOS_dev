using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IUpiSettingsRepository
    {
        Task<UpiSettings?> GetByBranchAsync(int branchId);
        Task<int> UpsertAsync(UpiSettings settings);
    }
}
