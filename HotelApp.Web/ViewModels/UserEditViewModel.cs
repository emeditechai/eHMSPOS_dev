using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels;

public class UserEditViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string BranchRolesJson { get; set; } = "{}";
    public int? Role { get; set; } // Keep for backward compatibility
    public List<int> SelectedRoleIds { get; set; } = new();
    public bool IsActive { get; set; }
    public List<int> SelectedBranchIds { get; set; } = new();
    public List<UserBranchRoleAssignment> ExistingBranchRoleAssignments { get; set; } = new();
    public List<Branch> AvailableBranches { get; set; } = new();
    public List<Role> AvailableRoles { get; set; } = new();
}
