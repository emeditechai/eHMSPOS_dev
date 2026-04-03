using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IB2BClientRepository
    {
        Task<IEnumerable<B2BClient>> GetByBranchAsync(int branchId);
        Task<IEnumerable<B2BClient>> GetActiveByBranchAsync(int branchId);
        Task<B2BClient?> GetByIdAsync(int id);
        Task<int> CreateAsync(B2BClient client);
        Task<bool> UpdateAsync(B2BClient client);
        Task<bool> CodeExistsAsync(string clientCode, int branchId, int? excludeId = null);
    }
}