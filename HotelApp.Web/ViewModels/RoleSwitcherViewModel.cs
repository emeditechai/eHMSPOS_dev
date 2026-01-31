using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels;

public class RoleSwitcherViewModel
{
    public List<Role> Roles { get; set; } = new();
    public int SelectedRoleId { get; set; }
    public string? SelectedRoleName { get; set; }
}
