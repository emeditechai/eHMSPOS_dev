using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace HotelApp.Web.Controllers
{
    public class BaseController : Controller
    {
        protected int CurrentBranchID { get; private set; }
        protected int? CurrentUserId { get; private set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // --- UserId: session first, fall back to auth claim ---
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue && sessionUserId.Value > 0)
            {
                CurrentUserId = sessionUserId.Value;
            }
            else
            {
                var claimValue = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(claimValue, out var claimUserId) && claimUserId > 0)
                {
                    CurrentUserId = claimUserId;
                    // Re-hydrate session so subsequent requests don't need the fallback
                    HttpContext.Session.SetInt32("UserId", claimUserId);
                }
            }

            // --- BranchID: session first, fall back to auth claim ---
            var sessionBranchId = HttpContext.Session.GetInt32("BranchID");
            if (sessionBranchId.HasValue && sessionBranchId.Value > 0)
            {
                CurrentBranchID = sessionBranchId.Value;
            }
            else
            {
                var claimBranch = HttpContext.User.FindFirstValue("BranchID");
                if (int.TryParse(claimBranch, out var claimBranchId) && claimBranchId > 0)
                {
                    CurrentBranchID = claimBranchId;
                    HttpContext.Session.SetInt32("BranchID", claimBranchId);
                }
                else
                {
                    CurrentBranchID = 1; // Default to Head Office
                }
            }

            // --- Role / Branch name ---
            var currentRoleId = HttpContext.Session.GetInt32("SelectedRoleId")
                ?? HttpContext.Session.GetInt32("RoleId")
                ?? (int.TryParse(HttpContext.User.FindFirstValue("SelectedRoleId"), out var cr) ? cr : (int?)null);
            var currentRoleName = HttpContext.Session.GetString("SelectedRoleName")
                ?? HttpContext.User.FindFirstValue("SelectedRoleName");
            var branchName = HttpContext.Session.GetString("BranchName")
                ?? HttpContext.User.FindFirstValue("BranchName")
                ?? "Head Office";

            ViewBag.CurrentBranchID = CurrentBranchID;
            ViewBag.CurrentUserId = CurrentUserId;
            ViewBag.CurrentBranchName = branchName;
            ViewBag.CurrentRoleId = currentRoleId;
            ViewBag.SelectedRoleId = currentRoleId;
            ViewBag.SelectedRoleName = currentRoleName;

            var isHO = HttpContext.Session.GetString("IsHOBranch");
            ViewBag.CurrentBranchIsHO = isHO == "1";
            ViewBag.CurrentRoleIsAdmin = currentRoleName?.Equals("Administrator", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>True when logged-in branch is marked IsHOBranch AND active role is Administrator.</summary>
        protected bool IsHOBranchAdmin =>
            (HttpContext.Session.GetString("IsHOBranch") == "1") &&
            (HttpContext.Session.GetString("SelectedRoleName")
                ?.Equals("Administrator", StringComparison.OrdinalIgnoreCase) == true);

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
