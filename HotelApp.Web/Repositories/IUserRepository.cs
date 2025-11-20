using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task UpdateLoginInfoAsync(int userId, bool success);
}
