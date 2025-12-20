using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<BrowserNotificationItem>> GetBranchNotificationsAsync(int branchId, DateTime today);
}
