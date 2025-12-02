using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Branch")]
    public int BranchID { get; set; }

    public string? ReturnUrl { get; set; }
    
    // For dropdown
    public List<Branch>? AvailableBranches { get; set; }
}
