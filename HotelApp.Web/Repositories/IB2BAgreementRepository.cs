using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IB2BAgreementRepository
    {
        Task<IEnumerable<B2BAgreement>> GetByBranchAsync(int branchId);
        Task<B2BAgreement?> GetByIdAsync(int id);
        Task<int> CreateAsync(B2BAgreement agreement);
        Task<bool> UpdateAsync(B2BAgreement agreement);
        Task<bool> CodeExistsAsync(string agreementCode, int branchId, int? excludeId = null);
    }
}