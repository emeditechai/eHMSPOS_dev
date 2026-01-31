namespace HotelApp.Web.Services;

public interface IAuthorizationMatrixService
{
    Task<bool> CanAccessPageAsync(int userId, int branchId, string controller, string action);
    Task<bool> CanAccessResourceKeyAsync(int userId, int branchId, string resourceKey);
}
