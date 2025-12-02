using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<int> CreateUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    Task UpdateLoginInfoAsync(int userId, bool success);
    Task UpdatePasswordAsync(int userId, string passwordHash, string salt);
}
