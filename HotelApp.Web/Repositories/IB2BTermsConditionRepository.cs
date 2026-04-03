using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IB2BTermsConditionRepository
    {
        Task<IEnumerable<B2BTermsCondition>> GetByBranchAsync(int branchId);
        Task<IEnumerable<B2BTermsCondition>> GetActiveByBranchAsync(int branchId);
        Task<B2BTermsCondition?> GetByIdAsync(int id);
        Task<int> CreateAsync(B2BTermsCondition termsCondition);
        Task<bool> UpdateAsync(B2BTermsCondition termsCondition);
        Task<bool> CodeExistsAsync(string termsCode, int branchId, int? excludeId = null);
    }
}