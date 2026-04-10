using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class AgreementMasterController : BaseController
    {
        private static readonly HashSet<string> AllowedSignedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
        };

        private const long SignedDocumentMaxBytes = 10 * 1024 * 1024;
        private static readonly IReadOnlyList<string> BillingTypes = new[] { "Credit", "Prepaid", "Postpaid" };
        private static readonly IReadOnlyList<string> BillingCycles = new[] { "Daily", "Weekly", "Monthly", "Quarterly" };
        private static readonly IReadOnlyList<string> RatePlanTypes = new[] { "Corporate Rate", "Contract Rate" };
        private static readonly IReadOnlyList<string> MealPlans = new[] { "EP", "CP", "MAP", "AP" };
        private static readonly IReadOnlyList<string> AgreementTypes = new[] { "Corporate", "Travel Agent", "OTA", "Event", "Crew" };
        private static readonly IReadOnlyList<string> ApprovalStatuses = new[] { "Draft", "Pending Approval", "Approved", "Expired", "Suspended" };

        private readonly IB2BAgreementRepository _agreementRepository;
        private readonly IB2BTermsConditionRepository _termsConditionRepository;
        private readonly ICancellationPolicyRepository _cancellationPolicyRepository;
        private readonly IGstSlabRepository _gstSlabRepository;
        private readonly IRoomTypeRepository _roomTypeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHotelSettingsRepository _hotelSettingsRepository;
        private readonly IWebHostEnvironment _environment;

        public AgreementMasterController(
            IB2BAgreementRepository agreementRepository,
            IB2BTermsConditionRepository termsConditionRepository,
            ICancellationPolicyRepository cancellationPolicyRepository,
            IGstSlabRepository gstSlabRepository,
            IRoomTypeRepository roomTypeRepository,
            IUserRepository userRepository,
            IHotelSettingsRepository hotelSettingsRepository,
            IWebHostEnvironment environment)
        {
            _agreementRepository = agreementRepository;
            _termsConditionRepository = termsConditionRepository;
            _cancellationPolicyRepository = cancellationPolicyRepository;
            _gstSlabRepository = gstSlabRepository;
            _roomTypeRepository = roomTypeRepository;
            _userRepository = userRepository;
            _hotelSettingsRepository = hotelSettingsRepository;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Agreement Master";
            var rows = await _agreementRepository.GetByBranchAsync(CurrentBranchID);
            return View(rows);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateLookupsAsync();
            ViewData["Title"] = "Create Agreement";
            return View(new B2BAgreement
            {
                EffectiveFrom = DateTime.Today,
                EffectiveTo = DateTime.Today.AddYears(1),
                BillingType = BillingTypes.First(),
                BillingCycle = BillingCycles.Last(),
                RatePlanType = RatePlanTypes.First(),
                AgreementType = AgreementTypes.First(),
                ApprovalStatus = ApprovalStatuses.First(),
                IsAmendmentAllowed = true,
                IsActive = true
                ,RoomRates = new List<B2BAgreementRoomRate>
                {
                    new()
                    {
                        ValidFrom = DateTime.Today,
                        ValidTo = DateTime.Today.AddYears(1),
                        IsActive = true
                    }
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(B2BAgreement model)
        {
            Normalize(model);
            await PopulateLookupsAsync();
            Validate(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _agreementRepository.CodeExistsAsync(model.AgreementCode, CurrentBranchID))
            {
                ModelState.AddModelError(nameof(model.AgreementCode), "Agreement code already exists in this branch.");
                return View(model);
            }

            model.BranchID = CurrentBranchID;
            model.CreatedBy = GetCurrentUserId();
            model.SignedDocumentPath = await SaveSignedDocumentAsync(model.SignedDocumentFile, null);
            await _agreementRepository.CreateAsync(model);
            TempData["SuccessMessage"] = "Agreement created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _agreementRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            if (!row.RoomRates.Any())
            {
                row.RoomRates.Add(new B2BAgreementRoomRate
                {
                    ValidFrom = row.EffectiveFrom,
                    ValidTo = row.EffectiveTo,
                    IsActive = true
                });
            }

            await PopulateLookupsAsync();
            ViewData["Title"] = "Edit Agreement";
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, B2BAgreement model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var existingAgreement = await _agreementRepository.GetByIdAsync(id);
            if (existingAgreement == null || existingAgreement.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            Normalize(model);
            model.SignedDocumentPath = string.IsNullOrWhiteSpace(model.SignedDocumentPath)
                ? existingAgreement.SignedDocumentPath
                : model.SignedDocumentPath;
            await PopulateLookupsAsync();
            Validate(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _agreementRepository.CodeExistsAsync(model.AgreementCode, CurrentBranchID, model.Id))
            {
                ModelState.AddModelError(nameof(model.AgreementCode), "Agreement code already exists in this branch.");
                return View(model);
            }

            model.BranchID = CurrentBranchID;
            model.UpdatedBy = GetCurrentUserId();
            model.SignedDocumentPath = await SaveSignedDocumentAsync(model.SignedDocumentFile, existingAgreement.SignedDocumentPath);
            await _agreementRepository.UpdateAsync(model);
            TempData["SuccessMessage"] = "Agreement updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var row = await _agreementRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            await PopulateLookupsAsync();
            ViewData["Title"] = "View Agreement";
            ViewBag.IsReadOnly = true;
            return View("Edit", row);
        }

        public async Task<IActionResult> Print(int id)
        {
            var row = await _agreementRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            var hotelSettings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);
            ViewBag.HotelSettings = hotelSettings;

            if (row.TermsConditionId.HasValue)
            {
                var terms = await _termsConditionRepository.GetByIdAsync(row.TermsConditionId.Value);
                ViewBag.TermsCondition = terms;
            }

            return View(row);
        }

        private async Task PopulateLookupsAsync()
        {
            ViewBag.BillingTypes = BillingTypes;
            ViewBag.BillingCycles = BillingCycles;
            ViewBag.RatePlanTypes = RatePlanTypes;
            ViewBag.MealPlans = MealPlans;
            ViewBag.AgreementTypes = AgreementTypes;
            ViewBag.ApprovalStatuses = ApprovalStatuses;
            ViewBag.TermsConditions = await _termsConditionRepository.GetActiveByBranchAsync(CurrentBranchID);
            ViewBag.CancellationPolicies = (await _cancellationPolicyRepository.GetByBranchAsync(CurrentBranchID)).Where(x => x.IsActive).ToList();
            ViewBag.GstSlabs = (await _gstSlabRepository.GetAllAsync()).Where(x => x.IsActive).OrderBy(x => x.SlabName).ToList();
            ViewBag.RoomTypes = (await _roomTypeRepository.GetByBranchAsync(CurrentBranchID)).Where(x => x.IsActive).OrderBy(x => x.TypeName).ToList();
            ViewBag.Approvers = await _userRepository.GetUsersByBranchAsync(CurrentBranchID);
        }

        private static void Normalize(B2BAgreement model)
        {
            model.AgreementCode = (model.AgreementCode ?? string.Empty).Trim();
            model.AgreementName = (model.AgreementName ?? string.Empty).Trim();
            model.ContractReference = string.IsNullOrWhiteSpace(model.ContractReference) ? null : model.ContractReference.Trim();
            model.AgreementType = (model.AgreementType ?? string.Empty).Trim();
            model.BillingType = (model.BillingType ?? string.Empty).Trim();
            model.BillingCycle = string.IsNullOrWhiteSpace(model.BillingCycle) ? null : model.BillingCycle.Trim();
            model.PaymentTerms = string.IsNullOrWhiteSpace(model.PaymentTerms) ? null : model.PaymentTerms.Trim();
            model.RatePlanType = (model.RatePlanType ?? string.Empty).Trim();
            model.MealPlan = string.IsNullOrWhiteSpace(model.MealPlan) ? null : model.MealPlan.Trim();
            model.SeasonalRateNotes = string.IsNullOrWhiteSpace(model.SeasonalRateNotes) ? null : model.SeasonalRateNotes.Trim();
            model.BlackoutDatesNotes = string.IsNullOrWhiteSpace(model.BlackoutDatesNotes) ? null : model.BlackoutDatesNotes.Trim();
            model.ServiceRemarks = string.IsNullOrWhiteSpace(model.ServiceRemarks) ? null : model.ServiceRemarks.Trim();
            model.ApprovalStatus = (model.ApprovalStatus ?? string.Empty).Trim();
            model.SignedByName = string.IsNullOrWhiteSpace(model.SignedByName) ? null : model.SignedByName.Trim();
            model.SignedDocumentPath = string.IsNullOrWhiteSpace(model.SignedDocumentPath) ? null : model.SignedDocumentPath.Trim();
            model.Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim();
            model.InternalRemarks = string.IsNullOrWhiteSpace(model.InternalRemarks) ? null : model.InternalRemarks.Trim();

            model.TermsConditionId = model.TermsConditionId.GetValueOrDefault() > 0 ? model.TermsConditionId : null;
            model.CancellationPolicyId = model.CancellationPolicyId.GetValueOrDefault() > 0 ? model.CancellationPolicyId : null;
            model.GstSlabId = model.GstSlabId.GetValueOrDefault() > 0 ? model.GstSlabId : null;
            model.ApprovedByUserId = model.ApprovedByUserId.GetValueOrDefault() > 0 ? model.ApprovedByUserId : null;

            if (!string.Equals(model.BillingType, "Credit", StringComparison.OrdinalIgnoreCase))
            {
                model.CreditDays = 0;
                model.CreditLimit = null;
            }

            if (!model.IsAmendmentAllowed)
            {
                model.AmendmentChargeAmount = null;
            }

            if (!model.AutoRenew)
            {
                model.RenewalNoticeDays = null;
            }

            if (!string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                model.ApprovedByUserId = null;
                model.ApprovedDate = null;
            }

            var rows = model.RoomRates ?? new List<B2BAgreementRoomRate>();
            model.RoomRates = rows
                .Where(row => row.RoomTypeId > 0
                    || row.BaseRate > 0
                    || row.ContractRate > 0
                    || row.ExtraPaxRate > 0
                    || !string.IsNullOrWhiteSpace(row.SeasonLabel)
                    || !string.IsNullOrWhiteSpace(row.Remarks)
                    || !string.IsNullOrWhiteSpace(row.MealPlan))
                .Select(row => new B2BAgreementRoomRate
                {
                    Id = row.Id,
                    AgreementId = row.AgreementId,
                    RoomTypeId = row.RoomTypeId,
                    SeasonLabel = string.IsNullOrWhiteSpace(row.SeasonLabel) ? null : row.SeasonLabel.Trim(),
                    ValidFrom = row.ValidFrom == default ? model.EffectiveFrom : row.ValidFrom,
                    ValidTo = row.ValidTo == default ? model.EffectiveTo : row.ValidTo,
                    BaseRate = row.BaseRate,
                    ContractRate = row.ContractRate,
                    ExtraPaxRate = row.ExtraPaxRate,
                    MealPlan = string.IsNullOrWhiteSpace(row.MealPlan) ? null : row.MealPlan.Trim(),
                    GstSlabId = row.GstSlabId.GetValueOrDefault() > 0 ? row.GstSlabId : null,
                    Remarks = string.IsNullOrWhiteSpace(row.Remarks) ? null : row.Remarks.Trim(),
                    IsActive = row.IsActive
                })
                .ToList();
        }

        private void Validate(B2BAgreement model)
        {
            ValidateSignedDocumentFile(model.SignedDocumentFile);

            if (model.EffectiveTo < model.EffectiveFrom)
            {
                ModelState.AddModelError(nameof(model.EffectiveTo), "Effective to date must be on or after effective from date.");
            }

            if (string.Equals(model.BillingType, "Credit", StringComparison.OrdinalIgnoreCase) && model.CreditDays <= 0)
            {
                ModelState.AddModelError(nameof(model.CreditDays), "Credit days must be greater than zero for credit agreements.");
            }

            if (model.AutoRenew && (!model.RenewalNoticeDays.HasValue || model.RenewalNoticeDays.Value <= 0))
            {
                ModelState.AddModelError(nameof(model.RenewalNoticeDays), "Renewal notice days are required when auto renew is enabled.");
            }

            if (string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                if (!model.ApprovedByUserId.HasValue)
                {
                    ModelState.AddModelError(nameof(model.ApprovedByUserId), "Approved by user is required when the agreement is approved.");
                }

                if (!model.ApprovedDate.HasValue)
                {
                    ModelState.AddModelError(nameof(model.ApprovedDate), "Approved date is required when the agreement is approved.");
                }
            }

            if (!model.RoomRates.Any())
            {
                ModelState.AddModelError(nameof(model.RoomRates), "At least one room rate row is required.");
            }

            for (var index = 0; index < model.RoomRates.Count; index++)
            {
                var row = model.RoomRates[index];
                var prefix = $"RoomRates[{index}]";

                if (row.RoomTypeId <= 0)
                {
                    ModelState.AddModelError($"{prefix}.RoomTypeId", "Room type is required.");
                }

                if (row.ValidTo < row.ValidFrom)
                {
                    ModelState.AddModelError($"{prefix}.ValidTo", "Valid to date must be on or after valid from date.");
                }

                if (row.ValidFrom < model.EffectiveFrom || row.ValidTo > model.EffectiveTo)
                {
                    ModelState.AddModelError($"{prefix}.ValidFrom", "Room rate validity must fall within the agreement period.");
                }

                if (row.ContractRate <= 0)
                {
                    ModelState.AddModelError($"{prefix}.ContractRate", "Contract rate must be greater than zero.");
                }


            }
        }

        private void ValidateSignedDocumentFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return;
            }

            if (file.Length > SignedDocumentMaxBytes)
            {
                ModelState.AddModelError(nameof(B2BAgreement.SignedDocumentFile), "Signed document must be 10 MB or smaller.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedSignedDocumentExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(B2BAgreement.SignedDocumentFile), "Allowed formats: PDF, DOC, DOCX, JPG, JPEG, PNG.");
            }
        }

        private async Task<string?> SaveSignedDocumentAsync(IFormFile? file, string? existingPath)
        {
            if (file == null || file.Length == 0)
            {
                return existingPath;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"agreement_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
            var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "agreements");
            Directory.CreateDirectory(uploadDirectory);

            var physicalPath = Path.Combine(uploadDirectory, fileName);
            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            DeleteSignedDocumentIfLocal(existingPath);
            return $"/uploads/agreements/{fileName}";
        }

        private void DeleteSignedDocumentIfLocal(string? existingPath)
        {
            if (string.IsNullOrWhiteSpace(existingPath)
                || !existingPath.StartsWith("/uploads/agreements/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = existingPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }
    }
}