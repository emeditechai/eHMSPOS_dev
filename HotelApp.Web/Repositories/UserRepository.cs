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
                PhoneNumber, Phone, FullName, Role, IsActive, IsLockedOut, 
                FailedLoginAttempts, LastLoginDate, CreatedDate, LastModifiedDate, 
                MustChangePassword, PasswordLastChanged, RequiresMFA
            FROM Users 
            WHERE (Username = @Username OR Email = @Username) AND IsActive = 1";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
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
}
