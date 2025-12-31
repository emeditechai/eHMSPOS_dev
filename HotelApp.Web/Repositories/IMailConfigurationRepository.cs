using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IMailConfigurationRepository
    {
        Task<MailConfiguration?> GetByBranchAsync(int branchId);
        Task<int> UpsertAsync(MailConfiguration settings);
    }
}
