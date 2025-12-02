namespace HotelApp.Web.Models;

public class UserRoleMapping
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedDate { get; set; }
    public int? AssignedBy { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime ModifiedDate { get; set; }
    
    // Navigation properties
    public Role? Role { get; set; }
}
