namespace HotelApp.Web.Models;

public class UserBranchRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BranchID { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Navigation properties (populated via JOINs)
    public string? BranchName { get; set; }
    public string? RoleName { get; set; }
}
