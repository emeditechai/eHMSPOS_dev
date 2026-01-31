using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IAuthorizationResourceRepository
{
    Task<IReadOnlyList<AuthorizationResource>> GetAllActiveAsync();
    Task<AuthorizationResource?> GetPageResourceAsync(string controller, string action);
    Task<AuthorizationResource?> GetByKeyAsync(string resourceKey);
    Task<int> CreateUiResourceAsync(string resourceKey, string title, int? parentResourceId, int sortOrder, int? createdBy);
}
