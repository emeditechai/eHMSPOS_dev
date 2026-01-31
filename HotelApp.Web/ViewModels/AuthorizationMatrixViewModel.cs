using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels;

public class AuthorizationMatrixViewModel
{
    public string ScopeType { get; set; } = "Role"; // Role | User
    public int ScopeId { get; set; }
    public int? BranchId { get; set; } // null = all branches

    public List<Role> Roles { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<Branch> Branches { get; set; } = new();

    public List<ResourceNodeVm> ResourceTree { get; set; } = new();

    // Create UI resource
    public string? NewUiKey { get; set; }
    public string? NewUiTitle { get; set; }
    public int? NewUiParentResourceId { get; set; }
}

public class ResourceNodeVm
{
    public int Id { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int SortOrder { get; set; }

    // Inherit | Allow | Deny
    public string Decision { get; set; } = "Inherit";

    public List<ResourceNodeVm> Children { get; set; } = new();
}
