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

        bool isValid = false;

        // Check if this is a BCrypt password (starts with $2)
        if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash.StartsWith("$2"))
        {
            // New BCrypt password validation
            try
            {
                isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                isValid = false;
            }
        }
        else if (!string.IsNullOrEmpty(user.Salt))
        {
            // Legacy PBKDF2 password validation
            try
            {
                var saltBytes = Convert.FromBase64String(user.Salt);
                using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, saltBytes, 10000, System.Security.Cryptography.HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(32);
                var computedHash = Convert.ToBase64String(hash);
                
                if (computedHash == user.PasswordHash)
                {
                    isValid = true;
                    // Migrate to BCrypt format
                    var newSalt = GenerateSalt();
                    var newHash = HashPassword(password, newSalt);
                    await _userRepository.UpdatePasswordAsync(user.Id, newHash, newSalt);
                }
            }
            catch
            {
                // If PBKDF2 validation fails, try plain text comparison
                if (user.PasswordHash == password)
                {
                    isValid = true;
                    var newSalt = GenerateSalt();
                    var newHash = HashPassword(password, newSalt);
                    await _userRepository.UpdatePasswordAsync(user.Id, newHash, newSalt);
                }
            }
        }
        else
        {
            // Plain text password (no salt)
            if (user.PasswordHash == password)
            {
                isValid = true;
                // Update to BCrypt format
                var newSalt = GenerateSalt();
                var newHash = HashPassword(password, newSalt);
                await _userRepository.UpdatePasswordAsync(user.Id, newHash, newSalt);
            }
        }
        
        // Update login info
        await _userRepository.UpdateLoginInfoAsync(user.Id, isValid);
        
        return isValid ? user : null;
    }

    private string GenerateSalt()
    {
        return BCrypt.Net.BCrypt.GenerateSalt(12);
    }

    private string HashPassword(string password, string salt)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, salt);
    }
}
