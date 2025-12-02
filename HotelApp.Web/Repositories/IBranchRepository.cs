using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IBranchRepository
    {
        Task<IEnumerable<Branch>> GetAllBranchesAsync();
        Task<IEnumerable<Branch>> GetActiveBranchesAsync();
        Task<Branch?> GetBranchByIdAsync(int id);
        Task<Branch?> GetBranchByCodeAsync(string branchCode);
        Task<Branch?> GetHOBranchAsync();
        Task<int> CreateBranchAsync(Branch branch);
        Task<bool> UpdateBranchAsync(Branch branch);
        Task<bool> DeleteBranchAsync(int id);
        Task<bool> BranchCodeExistsAsync(string branchCode, int? excludeId = null);
        Task<bool> BranchNameExistsAsync(string branchName, int? excludeId = null);
    }
}
