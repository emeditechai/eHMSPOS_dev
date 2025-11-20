using BCrypt.Net;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return null;

        // Check if account is locked
        if (user.IsLockedOut)
        {
            return null;
        }

        // Verify password using BCrypt
        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        
        // Update login info
        await _userRepository.UpdateLoginInfoAsync(user.Id, isValid);
        
        return isValid ? user : null;
    }
}
