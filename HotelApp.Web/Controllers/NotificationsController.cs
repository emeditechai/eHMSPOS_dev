using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers;

[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationRepository notificationRepository, ILogger<NotificationsController> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Poll()
    {
        var branchId = HttpContext.Session.GetInt32("BranchID");
        if (branchId is null or 0)
        {
            var claimBranchId = User.FindFirst("BranchID")?.Value;
            if (!int.TryParse(claimBranchId, out var parsed) || parsed <= 0)
            {
                return Json(new { serverTimeUtc = DateTime.UtcNow, items = Array.Empty<object>() });
            }
            branchId = parsed;
        }

        var allItems = await _notificationRepository.GetBranchNotificationsAsync(branchId.Value, DateTime.Today);

        // Filter role-restricted notifications
        var roleName = HttpContext.Session.GetString("SelectedRoleName")
            ?? User.FindFirst("SelectedRoleName")?.Value
            ?? string.Empty;

        var idMissingAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Administrator", "Manager", "Supervisor", "FrontDesk"
        };

        var items = allItems
            .Where(n => n.Kind != "id_missing" || idMissingAllowedRoles.Contains(roleName))
            .ToList();

        _logger.LogInformation("Notifications poll: BranchID={BranchId}, Role={Role}, Items={Count}",
            branchId.Value, roleName, items.Count);
        return Json(new { serverTimeUtc = DateTime.UtcNow, branchId = branchId.Value, items });
    }
}
