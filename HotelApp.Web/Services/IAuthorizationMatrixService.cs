namespace HotelApp.Web.Services;

public interface IAuthorizationMatrixService
{
    Task<bool> CanAccessPageAsync(int userId, int branchId, string controller, string action, int? selectedRoleId = null);
    Task<bool> CanAccessResourceKeyAsync(int userId, int branchId, string resourceKey, int? selectedRoleId = null);
}
