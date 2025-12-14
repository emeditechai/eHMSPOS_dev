using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IAmenityRepository
    {
        Task<IEnumerable<Amenity>> GetByBranchAsync(int branchId);
        Task<Amenity?> GetByIdAsync(int id);
        Task<int> CreateAsync(Amenity amenity);
        Task<bool> UpdateAsync(Amenity amenity);
        Task<bool> AmenityNameExistsAsync(string amenityName, int branchId, int? excludeId = null);
    }
}
