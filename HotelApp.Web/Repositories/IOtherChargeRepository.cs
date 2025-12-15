using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IOtherChargeRepository
    {
        Task<IEnumerable<OtherCharge>> GetByBranchAsync(int branchId);
        Task<OtherCharge?> GetByIdAsync(int id);
        Task<int> CreateAsync(OtherCharge otherCharge);
        Task<bool> UpdateAsync(OtherCharge otherCharge);
        Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null);
    }
}
