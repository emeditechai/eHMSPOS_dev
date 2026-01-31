using HotelApp.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.ViewModels;

public class SelectRoleViewModel
{
    public string? ReturnUrl { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public List<Role> AvailableRoles { get; set; } = new();

    [Required]
    [Display(Name = "Role")]
    public int SelectedRoleId { get; set; }
}
