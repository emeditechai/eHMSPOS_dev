namespace HotelApp.Web.Models;

public class NavMenuItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
