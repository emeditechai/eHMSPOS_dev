namespace HotelApp.Web.Models;

public class RoleNavMenuItem
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int NavMenuItemId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
