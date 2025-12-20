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

        var items = await _notificationRepository.GetBranchNotificationsAsync(branchId.Value, DateTime.Today);
        _logger.LogInformation("Notifications poll: BranchID={BranchId}, Items={Count}", branchId.Value, items.Count);
        return Json(new { serverTimeUtc = DateTime.UtcNow, branchId = branchId.Value, items });
    }
}
