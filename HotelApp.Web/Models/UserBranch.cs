namespace HotelApp.Web.Models;

public class UserBranch
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BranchID { get; set; }
    public bool IsActive { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    // Navigation properties
    public Branch? Branch { get; set; }
}
