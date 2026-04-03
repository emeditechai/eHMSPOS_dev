using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IGstSlabRepository
    {
        Task<IEnumerable<GstSlab>> GetAllAsync();
        Task<GstSlab?> GetByIdAsync(int id);
        Task<int> CreateAsync(GstSlab slab);
        Task<bool> UpdateAsync(GstSlab slab);
        Task<bool> CodeExistsAsync(string slabCode, int? excludeId = null);
        Task<GstSlabBand?> ResolveBandAsync(decimal tariffAmount, DateTime stayDate, int? gstSlabId = null);
    }
}