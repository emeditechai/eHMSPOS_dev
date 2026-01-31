using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HotelApp.Web.Controllers
{
    public class BaseController : Controller
    {
        protected int CurrentBranchID { get; private set; }
        protected int? CurrentUserId { get; private set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Get BranchID from session
            CurrentBranchID = HttpContext.Session.GetInt32("BranchID") ?? 1; // Default to HO branch
            CurrentUserId = HttpContext.Session.GetInt32("UserId");
            var currentRoleId = HttpContext.Session.GetInt32("SelectedRoleId") ?? HttpContext.Session.GetInt32("RoleId");
            var currentRoleName = HttpContext.Session.GetString("SelectedRoleName");
            
            var branchName = HttpContext.Session.GetString("BranchName") ?? "Head Office";

            // Store in ViewBag for use in views
            ViewBag.CurrentBranchID = CurrentBranchID;
            ViewBag.CurrentUserId = CurrentUserId;
            ViewBag.CurrentBranchName = branchName;
            ViewBag.CurrentRoleId = currentRoleId;
            ViewBag.SelectedRoleId = currentRoleId;
            ViewBag.SelectedRoleName = currentRoleName;
        }

        protected int GetCurrentBranchID()
        {
            return CurrentBranchID;
        }

        protected int? GetCurrentUserId()
        {
            return CurrentUserId;
        }

    }
}
