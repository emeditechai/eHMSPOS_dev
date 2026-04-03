using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class TermsConditionMasterController : BaseController
    {
        private static readonly IReadOnlyList<string> TermsTypes = new[] { "General", "Corporate", "Travel Agent", "OTA", "Event" };

        private readonly IB2BTermsConditionRepository _termsConditionRepository;
        private readonly ICancellationPolicyRepository _cancellationPolicyRepository;

        public TermsConditionMasterController(
            IB2BTermsConditionRepository termsConditionRepository,
            ICancellationPolicyRepository cancellationPolicyRepository)
        {
            _termsConditionRepository = termsConditionRepository;
            _cancellationPolicyRepository = cancellationPolicyRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Terms & Conditions Master";
            var rows = await _termsConditionRepository.GetByBranchAsync(CurrentBranchID);
            return View(rows);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateLookupsAsync();
            ViewData["Title"] = "Create Terms & Conditions";
            return View(new B2BTermsCondition
            {
                TermsType = TermsTypes.First(),
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(B2BTermsCondition model)
        {
            Normalize(model);
            await PopulateLookupsAsync();
            Validate(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _termsConditionRepository.CodeExistsAsync(model.TermsCode, CurrentBranchID))
            {
                ModelState.AddModelError(nameof(model.TermsCode), "Terms code already exists in this branch.");
                return View(model);
            }

            model.BranchID = CurrentBranchID;
            model.CreatedBy = GetCurrentUserId();
            await _termsConditionRepository.CreateAsync(model);
            TempData["SuccessMessage"] = "Terms & conditions created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _termsConditionRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            await PopulateLookupsAsync();
            ViewData["Title"] = "Edit Terms & Conditions";
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, B2BTermsCondition model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            Normalize(model);
            await PopulateLookupsAsync();
            Validate(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _termsConditionRepository.CodeExistsAsync(model.TermsCode, CurrentBranchID, model.Id))
            {
                ModelState.AddModelError(nameof(model.TermsCode), "Terms code already exists in this branch.");
                return View(model);
            }

            model.BranchID = CurrentBranchID;
            model.UpdatedBy = GetCurrentUserId();
            await _termsConditionRepository.UpdateAsync(model);
            TempData["SuccessMessage"] = "Terms & conditions updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var row = await _termsConditionRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            await PopulateLookupsAsync();
            ViewData["Title"] = "View Terms & Conditions";
            ViewBag.IsReadOnly = true;
            return View("Edit", row);
        }

        private async Task PopulateLookupsAsync()
        {
            ViewBag.TermsTypes = TermsTypes;
            ViewBag.CancellationPolicies = (await _cancellationPolicyRepository.GetByBranchAsync(CurrentBranchID)).Where(x => x.IsActive).ToList();
        }

        private static void Normalize(B2BTermsCondition model)
        {
            model.TermsCode = (model.TermsCode ?? string.Empty).Trim();
            model.TermsTitle = (model.TermsTitle ?? string.Empty).Trim();
            model.TermsType = (model.TermsType ?? string.Empty).Trim();
            model.PaymentTerms = string.IsNullOrWhiteSpace(model.PaymentTerms) ? null : model.PaymentTerms.Trim();
            model.RefundPolicy = string.IsNullOrWhiteSpace(model.RefundPolicy) ? null : model.RefundPolicy.Trim();
            model.NoShowPolicy = string.IsNullOrWhiteSpace(model.NoShowPolicy) ? null : model.NoShowPolicy.Trim();
            model.AmendmentPolicy = string.IsNullOrWhiteSpace(model.AmendmentPolicy) ? null : model.AmendmentPolicy.Trim();
            model.CheckInCheckOutPolicy = string.IsNullOrWhiteSpace(model.CheckInCheckOutPolicy) ? null : model.CheckInCheckOutPolicy.Trim();
            model.ChildPolicy = string.IsNullOrWhiteSpace(model.ChildPolicy) ? null : model.ChildPolicy.Trim();
            model.ExtraBedPolicy = string.IsNullOrWhiteSpace(model.ExtraBedPolicy) ? null : model.ExtraBedPolicy.Trim();
            model.BillingInstructions = string.IsNullOrWhiteSpace(model.BillingInstructions) ? null : model.BillingInstructions.Trim();
            model.TaxNotes = string.IsNullOrWhiteSpace(model.TaxNotes) ? null : model.TaxNotes.Trim();
            model.LegalDisclaimer = string.IsNullOrWhiteSpace(model.LegalDisclaimer) ? null : model.LegalDisclaimer.Trim();
            model.SpecialConditions = string.IsNullOrWhiteSpace(model.SpecialConditions) ? null : model.SpecialConditions.Trim();
            model.CancellationPolicyId = model.CancellationPolicyId.GetValueOrDefault() > 0 ? model.CancellationPolicyId : null;
        }

        private void Validate(B2BTermsCondition model)
        {
            if (string.IsNullOrWhiteSpace(model.TermsTitle))
            {
                ModelState.AddModelError(nameof(model.TermsTitle), "Terms title is required.");
            }
        }
    }
}