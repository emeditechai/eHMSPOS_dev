namespace HotelApp.Web.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Salt { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public int? Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLockedOut { get; set; } = false;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LastLoginDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public bool MustChangePassword { get; set; } = false;
    public DateTime? PasswordLastChanged { get; set; }
    public bool RequiresMFA { get; set; } = false;
}
