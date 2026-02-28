namespace HotelApp.Web.ViewModels;

public class UserProfileMenuViewModel
{
    public string DisplayName { get; set; } = "User";
    public string Email { get; set; } = string.Empty;
    public string ActiveRoleName { get; set; } = string.Empty;
    public bool CanSwitchRole { get; set; }
    public string? ProfilePicturePath { get; set; }
}
