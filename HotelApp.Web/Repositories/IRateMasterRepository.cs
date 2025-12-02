using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IRateMasterRepository
    {
        Task<IEnumerable<RateMaster>> GetAllAsync();
        Task<IEnumerable<RateMaster>> GetByBranchAsync(int branchId);
        Task<RateMaster?> GetByIdAsync(int id);
        Task<int> CreateAsync(RateMaster rate);
        Task<bool> UpdateAsync(RateMaster rate);
        Task<IEnumerable<RoomType>> GetRoomTypesAsync();
        Task<IEnumerable<string>> GetCustomerTypesAsync();
        Task<IEnumerable<string>> GetSourcesAsync();
    }
}
