using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class B2BClientMasterController : BaseController
    {
        private readonly IB2BClientRepository _clientRepository;
        private readonly IB2BAgreementRepository _agreementRepository;
        private readonly ILocationRepository _locationRepository;

        public B2BClientMasterController(
            IB2BClientRepository clientRepository,
            IB2BAgreementRepository agreementRepository,
            ILocationRepository locationRepository)
        {
            _clientRepository = clientRepository;
            _agreementRepository = agreementRepository;
            _locationRepository = locationRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "B2B Client Master";
            var rows = await _clientRepository.GetByBranchAsync(CurrentBranchID);
            return View(rows);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateFormOptionsAsync();
            ViewData["Title"] = "Create B2B Client";
            return View(new B2BClient
            {
                IsActive = true,
                CompanyType = "Corporate",
                BillingCycle = "Monthly",
                BillingType = "Prepaid",
                GstRegistrationType = "Regular"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(B2BClient model)
        {
            Normalize(model);
            await PopulateFormOptionsAsync(model.StateId, model.CountryId, model.PlaceOfSupplyStateId, model.AgreementId);
            Validate(model);

            await ValidateAgreementSelectionAsync(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _clientRepository.CodeExistsAsync(model.ClientCode, CurrentBranchID))
            {
                ModelState.AddModelError(nameof(model.ClientCode), "B2B code already exists in this branch.");
                return View(model);
            }

            model.BranchID = CurrentBranchID;
            model.CreatedBy = GetCurrentUserId();
            await _clientRepository.CreateAsync(model);
            TempData["SuccessMessage"] = "B2B client created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _clientRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            await PopulateFormOptionsAsync(row.StateId, row.CountryId, row.PlaceOfSupplyStateId, row.AgreementId);
            ViewData["Title"] = "Edit B2B Client";
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, B2BClient model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            Normalize(model);
            await PopulateFormOptionsAsync(model.StateId, model.CountryId, model.PlaceOfSupplyStateId, model.AgreementId);
            Validate(model);

            await ValidateAgreementSelectionAsync(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _clientRepository.CodeExistsAsync(model.ClientCode, CurrentBranchID, model.Id))
            {
                ModelState.AddModelError(nameof(model.ClientCode), "B2B code already exists in this branch.");
                return View(model);
            }

            model.BranchID = CurrentBranchID;
            model.UpdatedBy = GetCurrentUserId();
            await _clientRepository.UpdateAsync(model);
            TempData["SuccessMessage"] = "B2B client updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var row = await _clientRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            await PopulateFormOptionsAsync(row.StateId, row.CountryId, row.PlaceOfSupplyStateId, row.AgreementId);
            ViewData["Title"] = "View B2B Client";
            ViewBag.IsReadOnly = true;
            return View("Edit", row);
        }

        private async Task PopulateFormOptionsAsync(
            int? selectedStateId = null,
            int? selectedCountryId = null,
            int? placeOfSupplyStateId = null,
            int? selectedAgreementId = null)
        {
            var countries = (await _locationRepository.GetCountriesAsync()).ToList();
            var india = countries.FirstOrDefault(c => string.Equals(c.Code, "IN", StringComparison.OrdinalIgnoreCase))
                ?? countries.FirstOrDefault();

            var countryId = selectedCountryId.GetValueOrDefault(india?.Id ?? 0);
            if (countryId <= 0 && india != null)
            {
                countryId = india.Id;
            }

            var states = countryId > 0
                ? (await _locationRepository.GetStatesByCountryAsync(countryId)).ToList()
                : new List<State>();

            var selectedState = selectedStateId.GetValueOrDefault();
            var cities = selectedState > 0
                ? (await _locationRepository.GetCitiesByStateAsync(selectedState)).ToList()
                : new List<City>();

            ViewBag.Countries = countries;
            ViewBag.SelectedCountryId = countryId;
            ViewBag.States = states;
            ViewBag.Cities = cities;
            ViewBag.Agreements = (await _agreementRepository.GetByBranchAsync(CurrentBranchID))
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.AgreementName)
                .ToList();
            ViewBag.SelectedStateId = selectedStateId;
            ViewBag.PlaceOfSupplyStateId = placeOfSupplyStateId;
            ViewBag.SelectedAgreementId = selectedAgreementId;
            ViewBag.CompanyTypes = new[] { "Corporate", "Travel Agent", "OTA" };
            ViewBag.BillingCycles = new[] { "Daily", "Weekly", "Monthly" };
            ViewBag.BillingTypes = new[] { "Prepaid", "Credit" };
            ViewBag.GstRegistrationTypes = new[] { "Regular", "Composition" };
        }

        [HttpGet]
        public async Task<IActionResult> GetCities(int stateId)
        {
            if (stateId <= 0)
            {
                return Json(new { success = false, cities = Array.Empty<object>() });
            }

            var cities = await _locationRepository.GetCitiesByStateAsync(stateId);
            return Json(new
            {
                success = true,
                cities = cities.Select(c => new { c.Id, c.Name })
            });
        }

        private static void Normalize(B2BClient model)
        {
            model.ClientCode = (model.ClientCode ?? string.Empty).Trim();
            model.ClientName = (model.ClientName ?? string.Empty).Trim();
            model.DisplayName = (model.DisplayName ?? string.Empty).Trim();
            model.CompanyType = (model.CompanyType ?? string.Empty).Trim();
            model.AgreementId = model.AgreementId.GetValueOrDefault() > 0 ? model.AgreementId : null;
            model.Pan = (model.Pan ?? string.Empty).Trim().ToUpperInvariant();
            model.ContactPerson = (model.ContactPerson ?? string.Empty).Trim();
            model.ContactNo = (model.ContactNo ?? string.Empty).Trim();
            model.CorporateEmail = (model.CorporateEmail ?? string.Empty).Trim();
            model.AlternateContact = string.IsNullOrWhiteSpace(model.AlternateContact) ? null : model.AlternateContact.Trim();
            model.Address = (model.Address ?? string.Empty).Trim();
            model.AddressLine2 = string.IsNullOrWhiteSpace(model.AddressLine2) ? null : model.AddressLine2.Trim();
            model.City = (model.City ?? string.Empty).Trim();
            model.Pincode = (model.Pincode ?? string.Empty).Trim();
            model.BillingCycle = (model.BillingCycle ?? string.Empty).Trim();
            model.BillingType = (model.BillingType ?? string.Empty).Trim();
            model.GstNo = (model.GstNo ?? string.Empty).Trim().ToUpperInvariant();
            model.Cin = string.IsNullOrWhiteSpace(model.Cin) ? null : model.Cin.Trim().ToUpperInvariant();
            model.GstRegistrationType = (model.GstRegistrationType ?? string.Empty).Trim();
            model.Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim();

            model.IsCreditAllowed = string.Equals(model.BillingType, "Credit", StringComparison.OrdinalIgnoreCase);
            model.OutstandingAmount = model.OutstandingAmount < 0 ? 0 : model.OutstandingAmount;

            if (!model.IsCreditAllowed)
            {
                model.CreditAmount = null;
                model.CreditDays = 0;
                model.AllowExceedLimit = false;
            }

            if (!model.TdsApplicable)
            {
                model.TdsPercentage = null;
            }

            if (model.PlaceOfSupplyStateId <= 0)
            {
                model.PlaceOfSupplyStateId = model.StateId;
            }
        }

        private void Validate(B2BClient model)
        {
            if (!model.AgreementId.HasValue)
            {
                ModelState.AddModelError(nameof(model.AgreementId), "Agreement master is required.");
            }

            if (model.CountryId <= 0)
            {
                ModelState.AddModelError(nameof(model.CountryId), "Country is required.");
            }

            if (model.StateId <= 0)
            {
                ModelState.AddModelError(nameof(model.StateId), "State is required.");
            }

            if (model.PlaceOfSupplyStateId <= 0)
            {
                ModelState.AddModelError(nameof(model.PlaceOfSupplyStateId), "Place of supply is required.");
            }

            if (string.IsNullOrWhiteSpace(model.City))
            {
                ModelState.AddModelError(nameof(model.City), "City is required.");
            }

            if (model.IsCreditAllowed && (!model.CreditAmount.HasValue || model.CreditAmount.Value <= 0))
            {
                ModelState.AddModelError(nameof(model.CreditAmount), "Credit limit is required when billing type is credit.");
            }

            if (model.IsCreditAllowed && model.CreditDays <= 0)
            {
                ModelState.AddModelError(nameof(model.CreditDays), "Credit days are required when billing type is credit.");
            }

            if (model.TdsApplicable && (!model.TdsPercentage.HasValue || model.TdsPercentage.Value <= 0))
            {
                ModelState.AddModelError(nameof(model.TdsPercentage), "TDS percentage is required when TDS is applicable.");
            }
        }

        private async Task ValidateAgreementSelectionAsync(B2BClient model)
        {
            if (!model.AgreementId.HasValue)
            {
                return;
            }

            var agreement = await _agreementRepository.GetByIdAsync(model.AgreementId.Value);
            if (agreement == null || agreement.BranchID != CurrentBranchID || !agreement.IsActive)
            {
                ModelState.AddModelError(nameof(model.AgreementId), "Select a valid active agreement master.");
            }
        }
    }
}