using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IUserBranchRepository
{
    Task<IEnumerable<UserBranch>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Branch>> GetBranchesByUserIdAsync(int userId);
    Task AssignBranchesToUserAsync(int userId, List<int> branchIds, int? createdBy = null);
    Task RemoveBranchFromUserAsync(int userId, int branchId);
    Task<bool> HasAccessToBranchAsync(int userId, int branchId);
}
