using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IFloorRepository
    {
        Task<IEnumerable<Floor>> GetAllAsync();
        Task<IEnumerable<Floor>> GetByBranchAsync(int branchId);
        Task<Floor?> GetByIdAsync(int id);
        Task<int> CreateAsync(Floor floor);
        Task<bool> UpdateAsync(Floor floor);
        Task<bool> FloorNameExistsAsync(string floorName, int branchId, int? excludeId = null);
    }
}
