namespace HotelApp.Web.Models;

public class RoleDashboardConfig
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string DashboardController { get; set; } = string.Empty;
    public string DashboardAction { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }

    // Populated via JOIN — not mapped to a DB column directly
    public string RoleName { get; set; } = string.Empty;
}
