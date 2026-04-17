using HotelApp.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelApp.Web.ViewModels;

public class RoleDashboardConfigIndexViewModel
{
    public IEnumerable<RoleDashboardConfig> Configs { get; set; } = new List<RoleDashboardConfig>();
}

public class RoleDashboardConfigEditViewModel
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string DashboardController { get; set; } = string.Empty;
    public string DashboardAction { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public List<SelectListItem> AvailableDashboards { get; set; } = new();
}
