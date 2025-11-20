namespace HotelApp.Web.Models;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedDate { get; set; }
    public int? AssignedBy { get; set; }
}
