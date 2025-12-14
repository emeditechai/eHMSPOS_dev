using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IRoomTypeRepository
    {
        Task<IEnumerable<RoomType>> GetAllAsync();
        Task<IEnumerable<RoomType>> GetByBranchAsync(int branchId);
        Task<RoomType?> GetByIdAsync(int id);
        Task<int> CreateAsync(RoomType roomType);
        Task<bool> UpdateAsync(RoomType roomType);
        Task<bool> RoomTypeNameExistsAsync(string typeName, int branchId, int? excludeId = null);

        Task<IEnumerable<Amenity>> GetAmenitiesByBranchAsync(int branchId);
    }
}
