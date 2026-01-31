namespace HotelApp.Web.Models;

public class AuthorizationResource
{
    public int Id { get; set; }
    public string ResourceType { get; set; } = string.Empty; // Group | Page | Ui
    public string ResourceKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int? ParentResourceId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
