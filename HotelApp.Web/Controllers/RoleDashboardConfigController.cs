using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using HotelApp.Web.Models;

namespace HotelApp.Web.Controllers;

[Authorize]
public class RoleDashboardConfigController : BaseController
{
    private readonly IRoleDashboardConfigRepository _roleDashboardConfigRepository;

    // Known dashboard options — add more here as new dashboards are built
    private static readonly List<(string Controller, string Action, string Label)> KnownDashboards =
    [
        ("Dashboard",         "Index",     "Main Dashboard"),
        ("Rooms",             "Dashboard", "Room Status Dashboard"),
        ("CashierDashboard",  "Index",     "Payment Summary Dashboard"),
    ];

    public RoleDashboardConfigController(IRoleDashboardConfigRepository roleDashboardConfigRepository)
    {
        _roleDashboardConfigRepository = roleDashboardConfigRepository;
    }

    private bool IsAdminUser()
    {
        var username = User?.Identity?.Name ?? string.Empty;
        return username.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsAdminUser())
            return Forbid();

        var configs = await _roleDashboardConfigRepository.GetAllWithRoleNamesAsync();
        var model = new RoleDashboardConfigIndexViewModel { Configs = configs };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int roleId)
    {
        if (!IsAdminUser())
            return Forbid();

        var config = await _roleDashboardConfigRepository.GetByRoleIdAsync(roleId);
        if (config == null)
            return NotFound();

        var model = new RoleDashboardConfigEditViewModel
        {
            RoleId              = config.RoleId,
            RoleName            = config.RoleName,
            DashboardController = config.DashboardController,
            DashboardAction     = config.DashboardAction,
            DisplayName         = config.DisplayName,
            IsActive            = config.IsActive,
            AvailableDashboards = BuildDashboardSelectList(config.DashboardController, config.DashboardAction)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleDashboardConfigEditViewModel model)
    {
        if (!IsAdminUser())
            return Forbid();

        if (!ModelState.IsValid)
        {
            model.AvailableDashboards = BuildDashboardSelectList(model.DashboardController, model.DashboardAction);
            var existingConfig = await _roleDashboardConfigRepository.GetByRoleIdAsync(model.RoleId);
            model.RoleName = existingConfig?.RoleName ?? string.Empty;
            return View(model);
        }

        // Validate that the selected controller/action is a known option
        var selected = model.DashboardController + "/" + model.DashboardAction;
        var valid = KnownDashboards.Any(d => d.Controller + "/" + d.Action == selected);
        if (!valid)
        {
            ModelState.AddModelError(string.Empty, "Please select a valid dashboard.");
            model.AvailableDashboards = BuildDashboardSelectList(model.DashboardController, model.DashboardAction);
            var existingConfig2 = await _roleDashboardConfigRepository.GetByRoleIdAsync(model.RoleId);
            model.RoleName = existingConfig2?.RoleName ?? string.Empty;
            return View(model);
        }

        await _roleDashboardConfigRepository.UpdateAsync(new RoleDashboardConfig
        {
            RoleId              = model.RoleId,
            DashboardController = model.DashboardController,
            DashboardAction     = model.DashboardAction,
            DisplayName         = model.DisplayName,
            IsActive            = model.IsActive
        });

        TempData["SuccessMessage"] = $"Dashboard configuration updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static List<SelectListItem> BuildDashboardSelectList(string currentController, string currentAction)
    {
        return KnownDashboards
            .Select(d => new SelectListItem
            {
                Value    = $"{d.Controller}/{d.Action}",
                Text     = d.Label,
                Selected = d.Controller == currentController && d.Action == currentAction
            })
            .ToList();
    }
}
