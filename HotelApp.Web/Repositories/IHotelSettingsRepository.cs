using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IHotelSettingsRepository
    {
        Task<HotelSettings?> GetByBranchAsync(int branchId);
        Task<int> UpsertAsync(HotelSettings settings, int modifiedBy);
    }
}
