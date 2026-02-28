using Microsoft.AspNetCore.Http;

namespace HotelApp.Web.ViewModels;

public class UserCreateViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string BranchRolesJson { get; set; } = "{}";
    public int? Role { get; set; } // Keep for backward compatibility
    public List<int> SelectedRoleIds { get; set; } = new();
    public List<int> SelectedBranchIds { get; set; } = new();
    public List<HotelApp.Web.Models.Branch> AvailableBranches { get; set; } = new();
    public List<HotelApp.Web.Models.Role> AvailableRoles { get; set; } = new();
    public IFormFile? ProfilePictureFile { get; set; }
    /// <summary>When set, user is locked to this branch only (non-HO-Admin).</summary>
    public int? RestrictToBranchId { get; set; }
    /// <summary>Whether multi-branch assignment is allowed (HO-Admin only).</summary>
    public bool IsMultiBranchAllowed { get; set; } = true;
}
