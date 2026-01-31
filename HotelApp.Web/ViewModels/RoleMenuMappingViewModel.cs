using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels;

public class RoleMenuMappingViewModel
{
    public int RoleId { get; set; }
    public List<Role> Roles { get; set; } = new();
    public List<MenuNodeViewModel> MenuTree { get; set; } = new();
    public List<int> SelectedMenuIds { get; set; } = new();
}

public class MenuNodeViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int SortOrder { get; set; }
    public bool IsAssigned { get; set; }
    public List<MenuNodeViewModel> Children { get; set; } = new();
}
