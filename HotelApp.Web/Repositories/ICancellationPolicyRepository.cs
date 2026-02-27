using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface ICancellationPolicyRepository
{
    Task<IReadOnlyList<CancellationPolicy>> GetByBranchAsync(int branchId);
    Task<CancellationPolicy?> GetByIdAsync(int id);
    Task<IReadOnlyList<CancellationPolicyRule>> GetRulesAsync(int policyId);

    Task<int> CreateAsync(CancellationPolicy policy, IReadOnlyList<CancellationPolicyRule> rules, int performedBy);
    Task<bool> UpdateAsync(CancellationPolicy policy, IReadOnlyList<CancellationPolicyRule> rules, int performedBy);
    Task<bool> SetActiveAsync(int policyId, bool isActive, int performedBy);

    Task<CancellationPolicy?> GetApplicablePolicyAsync(int branchId, string source, string customerType, string rateType, DateTime checkInDate);
    Task<(int? policyId, string? snapshotJson)> GetApplicablePolicySnapshotAsync(int branchId, string source, string customerType, string rateType, DateTime checkInDate);
}
