using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"
            SELECT 
                Id, Username, Email, PasswordHash, Salt, FirstName, LastName, 
                PhoneNumber, Phone, FullName, Role, BranchID, IsActive, IsLockedOut, 
                FailedLoginAttempts, LastLoginDate, CreatedDate, LastModifiedDate, 
                MustChangePassword, PasswordLastChanged, RequiresMFA, ProfilePicturePath
            FROM Users 
            WHERE (Username = @Username OR Email = @Username) AND IsActive = 1";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }
    
    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                Id, Username, Email, PasswordHash, Salt, FirstName, LastName, 
                PhoneNumber, Phone, FullName, Role, BranchID, IsActive, IsLockedOut, 
                FailedLoginAttempts, LastLoginDate, CreatedDate, LastModifiedDate, 
                MustChangePassword, PasswordLastChanged, RequiresMFA, ProfilePicturePath
            FROM Users 
            WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        const string sql = @"
            SELECT 
                Id, Username, Email, FirstName, LastName, FullName, Role, 
                IsActive, LastLoginDate, CreatedDate
            FROM Users 
            ORDER BY CreatedDate DESC";
        return await _connection.QueryAsync<User>(sql);
    }

    public async Task<int> CreateUserAsync(User user)
    {
        const string sql = @"
            INSERT INTO Users (
                Username, Email, PasswordHash, Salt, FirstName, LastName, 
                FullName, PhoneNumber, Phone, Role, BranchID, IsActive, 
                CreatedDate, FailedLoginAttempts, IsLockedOut, MustChangePassword, ProfilePicturePath
            )
            VALUES (
                @Username, @Email, @PasswordHash, @Salt, @FirstName, @LastName,
                @FullName, @PhoneNumber, @Phone, @Role, @BranchID, @IsActive,
                GETDATE(), 0, 0, 0, @ProfilePicturePath
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        return await _connection.ExecuteScalarAsync<int>(sql, user);
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        string sql;
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            sql = @"
                UPDATE Users 
                SET Username = @Username, Email = @Email, PasswordHash = @PasswordHash, 
                    Salt = @Salt, FirstName = @FirstName, LastName = @LastName,
                    FullName = @FullName, PhoneNumber = @PhoneNumber, Phone = @Phone,
                    Role = @Role, IsActive = @IsActive, ProfilePicturePath = @ProfilePicturePath,
                    LastModifiedDate = GETDATE(), PasswordLastChanged = GETDATE()
                WHERE Id = @Id";
        }
        else
        {
            sql = @"
                UPDATE Users 
                SET Username = @Username, Email = @Email, FirstName = @FirstName, 
                    LastName = @LastName, FullName = @FullName, PhoneNumber = @PhoneNumber,
                    Phone = @Phone, Role = @Role, IsActive = @IsActive, 
                    ProfilePicturePath = @ProfilePicturePath, LastModifiedDate = GETDATE()
                WHERE Id = @Id";
        }
        
        var result = await _connection.ExecuteAsync(sql, user);
        return result > 0;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        const string sql = "UPDATE Users SET IsActive = 0, LastModifiedDate = GETDATE() WHERE Id = @Id";
        var result = await _connection.ExecuteAsync(sql, new { Id = id });
        return result > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
    {
        const string sql = "SELECT COUNT(1) FROM Users WHERE Username = @Username AND (@ExcludeUserId IS NULL OR Id != @ExcludeUserId)";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { Username = username, ExcludeUserId = excludeUserId });
        return count > 0;
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        const string sql = "SELECT COUNT(1) FROM Users WHERE Email = @Email AND (@ExcludeUserId IS NULL OR Id != @ExcludeUserId)";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { Email = email, ExcludeUserId = excludeUserId });
        return count > 0;
    }

    public async Task UpdateLoginInfoAsync(int userId, bool success)
    {
        if (success)
        {
            const string sql = @"
                UPDATE Users 
                SET LastLoginDate = @Now, FailedLoginAttempts = 0, IsLockedOut = 0
                WHERE Id = @UserId";
            await _connection.ExecuteAsync(sql, new { UserId = userId, Now = DateTime.Now });
        }
        else
        {
            const string sql = @"
                UPDATE Users 
                SET FailedLoginAttempts = FailedLoginAttempts + 1,
                    IsLockedOut = CASE WHEN FailedLoginAttempts >= 4 THEN 1 ELSE 0 END
                WHERE Id = @UserId";
            await _connection.ExecuteAsync(sql, new { UserId = userId });
        }
    }

    public async Task UpdatePasswordAsync(int userId, string passwordHash, string salt)
    {
        const string sql = @"
            UPDATE Users 
            SET PasswordHash = @PasswordHash, 
                Salt = @Salt, 
                PasswordLastChanged = GETDATE(),
                LastModifiedDate = GETDATE()
            WHERE Id = @UserId";
        await _connection.ExecuteAsync(sql, new { UserId = userId, PasswordHash = passwordHash, Salt = salt });
    }
}
