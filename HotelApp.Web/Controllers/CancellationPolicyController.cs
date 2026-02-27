using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers;

[Authorize]
public sealed class CancellationPolicyController : BaseController
{
    private static readonly IReadOnlyList<string> RateTypes = new[] { "Standard", "Discounted", "NonRefundable" };
    private static readonly IReadOnlyList<string> CustomerTypes = new[] { "B2C", "B2B" };
    private static readonly IReadOnlyList<string> Sources = new[] { "WalkIn", "Phone", "Website", "OTA", "Reference" };

    private readonly ICancellationPolicyRepository _repo;

    public CancellationPolicyController(ICancellationPolicyRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Cancellation Policies";
        var rows = await _repo.GetByBranchAsync(CurrentBranchID);
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Create Cancellation Policy";
        PopulateLookups();
        return View(new CancellationPolicyEditViewModel
        {
            BranchID = CurrentBranchID,
            IsActive = true,
            CustomerType = CustomerTypes.First(),
            BookingSource = Sources.First(),
            RateType = RateTypes.First(),
            Rules = new List<CancellationPolicyRuleEditRow>
            {
                new CancellationPolicyRuleEditRow { MinHoursBeforeCheckIn = 0, MaxHoursBeforeCheckIn = 24, RefundPercent = 0, FlatDeduction = 0, IsActive = true, SortOrder = 10 }
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CancellationPolicyEditViewModel model)
    {
        ViewData["Title"] = "Create Cancellation Policy";
        PopulateLookups();

        NormalizeRules(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.BranchID = CurrentBranchID;
        var performedBy = CurrentUserId ?? 0;

        var policy = model.ToPolicy();
        policy.BranchID = CurrentBranchID;
        var rules = model.ToRules();

        var id = await _repo.CreateAsync(policy, rules, performedBy);
        TempData["SuccessMessage"] = "Cancellation policy created successfully.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var policy = await _repo.GetByIdAsync(id);
        if (policy == null || policy.BranchID != CurrentBranchID)
        {
            TempData["ErrorMessage"] = "Policy not found.";
            return RedirectToAction(nameof(Index));
        }

        var rules = await _repo.GetRulesAsync(id);
        ViewData["Title"] = "Edit Cancellation Policy";
        PopulateLookups();
        return View(new CancellationPolicyEditViewModel(policy, rules));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CancellationPolicyEditViewModel model)
    {
        ViewData["Title"] = "Edit Cancellation Policy";
        PopulateLookups();

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null || existing.BranchID != CurrentBranchID)
        {
            TempData["ErrorMessage"] = "Policy not found.";
            return RedirectToAction(nameof(Index));
        }

        model.Id = id;
        model.BranchID = CurrentBranchID;

        NormalizeRules(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var performedBy = CurrentUserId ?? 0;
        var policy = model.ToPolicy();
        policy.Id = id;
        policy.BranchID = CurrentBranchID;
        var rules = model.ToRules();

        await _repo.UpdateAsync(policy, rules, performedBy);
        TempData["SuccessMessage"] = "Cancellation policy saved successfully.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        var policy = await _repo.GetByIdAsync(id);
        if (policy == null || policy.BranchID != CurrentBranchID)
        {
            return Json(new { success = false, message = "Policy not found" });
        }

        await _repo.SetActiveAsync(id, isActive, CurrentUserId ?? 0);
        return Json(new { success = true });
    }

    // Used by Booking Create UI.
    [HttpGet]
    public async Task<IActionResult> Applicable(int branchId, string source, string customerType, string rateType, DateTime checkInDate)
    {
        var effectiveBranchId = branchId > 0 ? branchId : CurrentBranchID;

        var (policyId, snapshotJson) = await _repo.GetApplicablePolicySnapshotAsync(
            effectiveBranchId,
            source ?? string.Empty,
            customerType ?? string.Empty,
            rateType ?? "Standard",
            checkInDate);

        if (!policyId.HasValue)
        {
            return Json(new { success = true, found = false });
        }

        return Json(new { success = true, found = true, policyId = policyId.Value, snapshotJson });
    }

    private void PopulateLookups()
    {
        ViewBag.RateTypes = RateTypes;
        ViewBag.CustomerTypes = CustomerTypes;
        ViewBag.Sources = Sources;
    }

    private void NormalizeRules(CancellationPolicyEditViewModel model)
    {
        model.Rules ??= new List<CancellationPolicyRuleEditRow>();
        model.Rules = model.Rules
            .Where(r => r != null)
            .Select((r, idx) =>
            {
                r.MinHoursBeforeCheckIn = Math.Max(0, r.MinHoursBeforeCheckIn);
                r.MaxHoursBeforeCheckIn = Math.Max(0, r.MaxHoursBeforeCheckIn);
                r.RefundPercent = Math.Max(0, r.RefundPercent);
                r.FlatDeduction = Math.Max(0, r.FlatDeduction);
                r.SortOrder = r.SortOrder != 0 ? r.SortOrder : (idx + 1) * 10;
                return r;
            })
            .ToList();

        if (model.Rules.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Rules), "At least one rule is required.");
            return;
        }

        foreach (var r in model.Rules)
        {
            if (r.MaxHoursBeforeCheckIn < r.MinHoursBeforeCheckIn)
            {
                ModelState.AddModelError(nameof(model.Rules), "Rule range is invalid (Max < Min).");
                break;
            }
        }
    }
}
