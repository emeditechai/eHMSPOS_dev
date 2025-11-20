using HotelApp.Web.Models;

namespace HotelApp.Web.Services;

public interface IAuthService
{
    Task<User?> ValidateCredentialsAsync(string username, string password);
}
