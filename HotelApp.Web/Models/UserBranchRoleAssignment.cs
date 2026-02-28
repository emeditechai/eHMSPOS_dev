namespace HotelApp.Web.Models;

public class UserBranchRoleAssignment
{
    public int BranchId { get; set; }
    public List<int> RoleIds { get; set; } = new();
}
