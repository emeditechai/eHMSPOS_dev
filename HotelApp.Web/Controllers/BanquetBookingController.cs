using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.Services;
using HotelApp.Web.ViewModels;
using System.Text.Json;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class BanquetBookingController : BaseController
    {
        private readonly IBanquetBookingRepository _bookingRepo;
        private readonly IBanquetVenueRepository _venueRepo;
        private readonly IBanquetEventTypeRepository _eventTypeRepo;
        private readonly IBanquetPackageRepository _packageRepo;
        private readonly IBanquetAddonServiceRepository _addonRepo;
        private readonly IBanquetCancellationRepository _cancellationRepo;
        private readonly IBanquetBookingNumberService _numberService;
        private readonly IBanquetGSTService _gstService;
        private readonly IB2BClientRepository _b2bClientRepo;
        private readonly IB2BAgreementRepository _b2bAgreementRepo;
        private readonly IBankRepository _bankRepo;
        private readonly ICancellationPolicyRepository _cancellationPolicyRepo;
        private readonly IHotelSettingsRepository _hotelSettingsRepo;

        public BanquetBookingController(
            IBanquetBookingRepository bookingRepo,
            IBanquetVenueRepository venueRepo,
            IBanquetEventTypeRepository eventTypeRepo,
            IBanquetPackageRepository packageRepo,
            IBanquetAddonServiceRepository addonRepo,
            IBanquetCancellationRepository cancellationRepo,
            IBanquetBookingNumberService numberService,
            IBanquetGSTService gstService,
            IB2BClientRepository b2bClientRepo,
            IB2BAgreementRepository b2bAgreementRepo,
            IBankRepository bankRepo,
            ICancellationPolicyRepository cancellationPolicyRepo,
            IHotelSettingsRepository hotelSettingsRepo)
        {
            _bookingRepo           = bookingRepo;
            _venueRepo             = venueRepo;
            _eventTypeRepo         = eventTypeRepo;
            _packageRepo           = packageRepo;
            _addonRepo             = addonRepo;
            _cancellationRepo      = cancellationRepo;
            _numberService         = numberService;
            _gstService            = gstService;
            _b2bClientRepo         = b2bClientRepo;
            _b2bAgreementRepo      = b2bAgreementRepo;
            _bankRepo              = bankRepo;
            _cancellationPolicyRepo = cancellationPolicyRepo;
            _hotelSettingsRepo     = hotelSettingsRepo;
        }

        // ── Dashboard ─────────────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var kpi = await _bookingRepo.GetDashboardKpiAsync(CurrentBranchID);
            var vm = new BanquetDashboardViewModel
            {
                TodaysEvents           = kpi.TodaysEvents,
                UpcomingEvents7Days    = kpi.UpcomingEvents7Days,
                PendingConfirmations   = kpi.PendingConfirmations,
                ThisMonthRevenue       = kpi.ThisMonthRevenue,
                OutstandingBalance     = kpi.OutstandingBalance,
                ThisMonthBookings      = kpi.ThisMonthBookings,
                TodaysEventList        = kpi.TodaysEventList,
                RecentBookings         = kpi.RecentBookings
            };
            ViewData["Title"] = "Banquet Dashboard";
            return View(vm);
        }

        // ── Booking List ──────────────────────────────────────────────────────

        public async Task<IActionResult> List(string? status, DateOnly? fromDate, DateOnly? toDate, int? venueId, string? customerType)
        {
            var bookings = await _bookingRepo.GetListAsync(CurrentBranchID, status, fromDate, toDate, venueId, customerType, null);
            var venues   = await _venueRepo.GetByBranchAsync(CurrentBranchID);
            var vm = new BanquetBookingListViewModel
            {
                Bookings           = bookings,
                FilterStatus       = status,
                FromDate           = fromDate,
                ToDate             = toDate,
                FilterVenueId      = venueId,
                FilterCustomerType = customerType,
                Venues             = venues
            };
            ViewData["Title"] = "Event Bookings";
            return View(vm);
        }

        // ── Create ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Create()
        {
            await PopulateCreateViewBagAsync();
            ViewData["Title"] = "New Event Booking";
            return View(new BanquetBookingCreateViewModel
            {
                EventDate       = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                AttendeeCount   = 50,
                GuaranteePax    = 50,
                MealType        = "Veg",
                VenueHireType   = "FullDay",
                CustomerType    = "B2C",
                BillingTo       = "Guest"
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BanquetBookingCreateViewModel vm)
        {
            // Remove ModelState errors that don't apply to the active customer type.
            // B2C fields (GuestPhone/GuestName) are [Required] but should not fire for B2B,
            // and B2B-only fields should not fire for B2C.
            if (vm.CustomerType == "B2B")
            {
                ModelState.Remove("GuestPhone");   // Contact phone is optional in B2B
                ModelState.Remove("GuestEmail");
                ModelState.Remove("GuestGSTIN");
                ModelState.Remove("GuestAddress");
            }
            else
            {
                ModelState.Remove("B2BClientId");
                ModelState.Remove("B2BAgreementId");
                ModelState.Remove("CompanyName");
                ModelState.Remove("CompanyGSTIN");
                ModelState.Remove("CompanyPAN");
                ModelState.Remove("CreditDays");
                ModelState.Remove("BillingTo");
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateViewBagAsync();
                return View(vm);
            }

            // Venue availability check
            var stTime = TimeOnly.TryParse(vm.EventStartTime, out var st) ? st : (TimeOnly?)null;
            var enTime = TimeOnly.TryParse(vm.EventEndTime,   out var et) ? et : (TimeOnly?)null;
            var available = await _venueRepo.IsVenueAvailableAsync(vm.VenueId, vm.EventDate, stTime, enTime);
            if (!available)
            {
                ModelState.AddModelError("VenueId", "The selected venue is not available for this date/time slot. Please choose another venue or time.");
                await PopulateCreateViewBagAsync();
                return View(vm);
            }

            // Load masters for calculation
            var venue    = await _venueRepo.GetByIdAsync(vm.VenueId);
            BanquetPackage? package = vm.PackageId.HasValue ? await _packageRepo.GetByIdAsync(vm.PackageId.Value) : null;

            if (venue == null)
            {
                ModelState.AddModelError("VenueId", "Selected venue not found.");
                await PopulateCreateViewBagAsync();
                return View(vm);
            }

            // Build booking
            var bookingNumber = await _numberService.GenerateAsync(CurrentBranchID);
            var booking = new BanquetBooking
            {
                BanquetBookingNumber = bookingNumber,
                BranchID             = CurrentBranchID,
                EventDate            = vm.EventDate,
                EventEndDate         = vm.EventEndDate,
                EventStartTime       = stTime,
                EventEndTime         = enTime,
                SetupTime            = TimeOnly.TryParse(vm.SetupTime,    out var sut) ? sut : null,
                TeardownTime         = TimeOnly.TryParse(vm.TeardownTime, out var tdt) ? tdt : null,
                VenueId              = vm.VenueId,
                EventTypeId          = vm.EventTypeId,
                EventName            = vm.EventName,
                AttendeeCount        = vm.AttendeeCount,
                GuaranteePax         = vm.GuaranteePax,
                ChildCount           = vm.ChildCount,
                MealType             = vm.MealType,
                CustomerType         = vm.CustomerType,
                PrimaryGuestId       = vm.PrimaryGuestId,
                GuestName            = vm.GuestName,
                GuestPhone           = vm.GuestPhone,
                GuestEmail           = vm.GuestEmail,
                GuestAddress         = vm.GuestAddress,
                GuestGSTIN           = vm.GuestGSTIN,
                B2BClientId          = vm.CustomerType == "B2B" ? vm.B2BClientId : null,
                B2BAgreementId       = vm.CustomerType == "B2B" ? vm.B2BAgreementId : null,
                CompanyName          = vm.CompanyName,
                CompanyGSTIN         = vm.CompanyGSTIN,
                CompanyPAN           = vm.CompanyPAN,
                CompanyAddress       = vm.CompanyAddress,
                BillingTo            = vm.BillingTo,
                CreditDays           = vm.CreditDays,
                IsInterState         = vm.IsInterState,
                VenueHireType        = vm.VenueHireType,
                PackageId            = vm.PackageId,
                PackageTotalPax      = vm.PackageTotalPax,
                CancellationPolicyId = vm.CancellationPolicyId,
                SpecialRequests      = vm.SpecialRequests,
                InternalNotes        = vm.InternalNotes,
                Status               = "Inquiry",
                PaymentStatus        = "Pending",
                ApprovalStatus       = vm.CustomerType == "B2B" ? "Draft" : "Approved",
                DiscountAmount       = vm.DiscountAmount,
                ServiceChargeAmount  = vm.ServiceChargeAmount,
                CreatedBy            = CurrentUserId,
                LinkedHotelBookingId = vm.LinkedHotelBookingId
            };

            // Venue GST
            var venueRate = vm.VenueHireType == "HalfDay" ? venue.BaseRatePerHalfDay : venue.BaseRatePerDay;
            var venueGst  = _gstService.CalculateVenueGST(venue, venueRate, vm.IsInterState);
            booking.VenueBaseAmount  = venueGst.BaseAmount;
            booking.VenueGSTAmount   = venueGst.GSTAmount;
            booking.VenueCGSTAmount  = venueGst.CGSTAmount;
            booking.VenueSGSTAmount  = venueGst.SGSTAmount;
            booking.VenueIGSTAmount  = venueGst.IGSTAmount;

            // Package lines
            var packageLines = new List<BanquetBookingPackageLine>();
            if (package != null)
            {
                var pax        = vm.PackageTotalPax > 0 ? vm.PackageTotalPax : vm.AttendeeCount;
                var pkgBase    = package.PricePerPax * pax;
                var pkgGst     = _gstService.CalculatePackageGST(package, pkgBase, vm.IsInterState);

                booking.PackagePricePerPax = package.PricePerPax;
                booking.PackageBaseAmount  = pkgGst.BaseAmount;
                booking.PackageGSTAmount   = pkgGst.GSTAmount;
                booking.PackageCGSTAmount  = pkgGst.CGSTAmount;
                booking.PackageSGSTAmount  = pkgGst.SGSTAmount;
                booking.PackageIGSTAmount  = pkgGst.IGSTAmount;

                packageLines.Add(new BanquetBookingPackageLine
                {
                    PackageId       = package.Id,
                    PackageName     = package.PackageName,
                    PackageType     = package.PackageType,
                    MealType        = vm.MealType,
                    PricePerPax     = package.PricePerPax,
                    Pax             = pax,
                    BaseAmount      = pkgGst.BaseAmount,
                    GSTPercent      = pkgGst.GSTPercent,
                    CGSTPercent     = pkgGst.CGSTPercent,
                    SGSTPercent     = pkgGst.SGSTPercent,
                    IGSTPercent     = pkgGst.IGSTPercent,
                    GSTAmount       = pkgGst.GSTAmount,
                    CGSTAmount      = pkgGst.CGSTAmount,
                    SGSTAmount      = pkgGst.SGSTAmount,
                    IGSTAmount      = pkgGst.IGSTAmount,
                    TotalAmount     = pkgGst.TotalAmount,
                    MenuDescription = package.MenuDescription,
                    SACCode         = package.SACCode
                });
            }

            // Addon lines
            var addonLines  = new List<BanquetBookingAddonLine>();
            decimal addonBase = 0, addonGst = 0, addonCgst = 0, addonSgst = 0, addonIgst = 0;

            if (!string.IsNullOrWhiteSpace(vm.AddonLinesJson))
            {
                var addonInputs = JsonSerializer.Deserialize<List<BanquetAddonLineInput>>(vm.AddonLinesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                foreach (var input in addonInputs)
                {
                    var svc = await _addonRepo.GetByIdAsync(input.AddonServiceId);
                    if (svc == null) continue;

                    var lineBase = svc.Rate * input.Qty;
                    var lineGst  = _gstService.CalculateAddonGST(svc, lineBase, vm.IsInterState);

                    addonBase += lineGst.BaseAmount;
                    addonGst  += lineGst.GSTAmount;
                    addonCgst += lineGst.CGSTAmount;
                    addonSgst += lineGst.SGSTAmount;
                    addonIgst += lineGst.IGSTAmount;

                    addonLines.Add(new BanquetBookingAddonLine
                    {
                        AddonServiceId = svc.Id,
                        ServiceName    = svc.ServiceName,
                        ServiceType    = svc.ServiceType,
                        Rate           = svc.Rate,
                        RateType       = svc.RateType,
                        Qty            = input.Qty,
                        BaseAmount     = lineGst.BaseAmount,
                        GSTPercent     = lineGst.GSTPercent,
                        CGSTPercent    = lineGst.CGSTPercent,
                        SGSTPercent    = lineGst.SGSTPercent,
                        IGSTPercent    = lineGst.IGSTPercent,
                        GSTAmount      = lineGst.GSTAmount,
                        CGSTAmount     = lineGst.CGSTAmount,
                        SGSTAmount     = lineGst.SGSTAmount,
                        IGSTAmount     = lineGst.IGSTAmount,
                        TotalAmount    = lineGst.TotalAmount,
                        Notes          = input.Notes,
                        SACCode        = svc.SACCode
                    });
                }
            }

            booking.AddonBaseAmount  = addonBase;
            booking.AddonGSTAmount   = addonGst;
            booking.AddonCGSTAmount  = addonCgst;
            booking.AddonSGSTAmount  = addonSgst;
            booking.AddonIGSTAmount  = addonIgst;

            // Grand totals
            booking.TotalBaseAmount  = booking.VenueBaseAmount + booking.PackageBaseAmount + booking.AddonBaseAmount;
            booking.TotalGSTAmount   = booking.VenueGSTAmount  + booking.PackageGSTAmount  + booking.AddonGSTAmount;
            booking.TotalCGSTAmount  = booking.VenueCGSTAmount + booking.PackageCGSTAmount + booking.AddonCGSTAmount;
            booking.TotalSGSTAmount  = booking.VenueSGSTAmount + booking.PackageSGSTAmount + booking.AddonSGSTAmount;
            booking.TotalIGSTAmount  = booking.VenueIGSTAmount + booking.PackageIGSTAmount + booking.AddonIGSTAmount;

            var preTax = booking.TotalBaseAmount + booking.ServiceChargeAmount - booking.DiscountAmount + booking.TotalGSTAmount;
            booking.RoundOffAmount = vm.ApplyRoundOff ? Math.Round(preTax) - preTax : 0;
            booking.TotalAmount    = preTax + booking.RoundOffAmount;
            booking.BalanceAmount  = booking.TotalAmount;

            var bookingId = await _bookingRepo.CreateAsync(booking, packageLines, addonLines, null);

            // If advance amount was entered, redirect to head-wise payment allocation
            if (vm.AdvancePaymentAmount > 0)
            {
                TempData["InfoMessage"] = $"Booking {bookingNumber} created. Please allocate the advance payment of ₹{vm.AdvancePaymentAmount:N2} across the billing heads below.";
                return RedirectToAction(nameof(AddPayment), new { id = bookingId, initialAmount = vm.AdvancePaymentAmount });
            }

            TempData["SuccessMessage"] = $"Banquet booking {bookingNumber} created successfully!";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }

        // ── Details ───────────────────────────────────────────────────────────

        public async Task<IActionResult> Details(int id)
        {
            await _bookingRepo.RecalculateBalanceAsync(id);
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status == "Cancelled")
                ViewBag.Cancellation = await _cancellationRepo.GetByBookingIdAsync(id);

            // Provide masters for the Edit Package / Add Addon modals
            ViewBag.Packages = await _packageRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.Addons   = await _addonRepo.GetByBranchAsync(CurrentBranchID);

            ViewData["Title"] = $"Booking – {booking.BanquetBookingNumber}";
            return View(booking);
        }

        // ── Add Payment ───────────────────────────────────────────────────────

        public async Task<IActionResult> AddPayment(int id, decimal? initialAmount = null)
        {
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null) return NotFound();

            var due = await _bookingRepo.GetHeadWiseDueAsync(id);

            var vm = new BanquetAddPaymentViewModel
            {
                BanquetBookingId     = booking.Id,
                BanquetBookingNumber = booking.BanquetBookingNumber,
                EventName            = booking.EventName,
                TotalAmount          = booking.TotalAmount,
                BalanceAmount        = booking.BalanceAmount,
                Amount               = booking.BalanceAmount,
                VenueTotal           = due.VenueTotal,
                VenueDue             = due.VenueDue,
                PackageTotal         = due.PackageTotal,
                PackageDue           = due.PackageDue,
                AddonTotal           = due.AddonTotal,
                AddonDue             = due.AddonDue,
            };

            if (initialAmount.HasValue && initialAmount.Value > 0)
                ViewBag.InitialAmount = initialAmount.Value;

            ViewBag.Banks = await _bankRepo.GetAllActiveAsync();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(BanquetAddPaymentViewModel vm)
        {
            // Remove Amount from required validation – we drive from head allocations
            ModelState.Remove(nameof(vm.Amount));

            // Parse head allocations JSON  [{code:"V",amount:5000},{code:"P",amount:3000}]
            var allocations = new List<(string Code, decimal Amount)>();
            if (!string.IsNullOrWhiteSpace(vm.HeadAllocations))
            {
                try
                {
                    var raw = JsonSerializer.Deserialize<List<JsonElement>>(vm.HeadAllocations,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (raw != null)
                        foreach (var el in raw)
                        {
                            var code = el.GetProperty("code").GetString()?.Trim().ToUpperInvariant() ?? "";
                            var amt  = el.GetProperty("amount").GetDecimal();
                            if ((code == "V" || code == "P" || code == "A") && amt > 0)
                                allocations.Add((code, Math.Round(amt, 2, MidpointRounding.AwayFromZero)));
                        }
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Invalid head allocation data. Please retry.");
                }
            }

            if (!allocations.Any())
            {
                ModelState.AddModelError(string.Empty, "Please enter an amount for at least one billing head.");
            }

            if (!ModelState.IsValid)
            {
                var due = await _bookingRepo.GetHeadWiseDueAsync(vm.BanquetBookingId);
                vm.VenueTotal = due.VenueTotal; vm.VenueDue = due.VenueDue;
                vm.PackageTotal = due.PackageTotal; vm.PackageDue = due.PackageDue;
                vm.AddonTotal = due.AddonTotal; vm.AddonDue = due.AddonDue;
                ViewBag.Banks = await _bankRepo.GetAllActiveAsync();
                return View(vm);
            }

            var grossAmount = allocations.Sum(a => a.Amount);

            // Compute discount
            decimal discountAmount;
            decimal? discountPercent = null;
            switch (vm.DiscountType)
            {
                case "Percent":
                    discountPercent = Math.Max(0, Math.Min(100, vm.DiscountPercent ?? 0));
                    discountAmount  = Math.Round(grossAmount * discountPercent.Value / 100m, 2, MidpointRounding.AwayFromZero);
                    break;
                case "Flat":
                    discountAmount = Math.Round(Math.Max(0, Math.Min(grossAmount, vm.DiscountAmount)), 2, MidpointRounding.AwayFromZero);
                    break;
                default:
                    discountAmount = 0m;
                    break;
            }

            var netTotal = Math.Round(grossAmount - discountAmount + vm.RoundOffAmount, 2, MidpointRounding.AwayFromZero);
            if (netTotal < 0)
            {
                ModelState.AddModelError(string.Empty, "Net payable cannot be negative.");
                var due2 = await _bookingRepo.GetHeadWiseDueAsync(vm.BanquetBookingId);
                vm.VenueTotal = due2.VenueTotal; vm.VenueDue = due2.VenueDue;
                vm.PackageTotal = due2.PackageTotal; vm.PackageDue = due2.PackageDue;
                vm.AddonTotal = due2.AddonTotal; vm.AddonDue = due2.AddonDue;
                ViewBag.Banks = await _bankRepo.GetAllActiveAsync();
                return View(vm);
            }

            // Generate receipt group number (shared across all head rows for this payment)
            var receiptGroupNum = await _bookingRepo.GenerateNextReceiptNumberAsync(CurrentBranchID);

            // Distribute discount proportionally across heads; round-off goes to last
            var discountRemaining  = discountAmount;
            var roundOffRemaining  = vm.RoundOffAmount;
            var paidOn             = DateTime.UtcNow;

            for (var i = 0; i < allocations.Count; i++)
            {
                var (code, headGross) = allocations[i];
                var isLast = i == allocations.Count - 1;

                var discShare = isLast
                    ? discountRemaining
                    : Math.Round(discountAmount > 0 ? discountAmount * headGross / grossAmount : 0m, 2, MidpointRounding.AwayFromZero);
                discountRemaining -= discShare;

                var roShare = isLast ? roundOffRemaining : 0m;

                var netShare = headGross - discShare + roShare;

                var payment = new BanquetBookingPayment
                {
                    BanquetBookingId   = vm.BanquetBookingId,
                    ReceiptNumber      = receiptGroupNum,   // all heads share ONE receipt number
                    ReceiptGroupNumber = receiptGroupNum,
                    Amount             = Math.Round(netShare, 2, MidpointRounding.AwayFromZero),
                    PaymentMethod      = vm.PaymentMethod,
                    PaymentReference   = vm.PaymentReference,
                    BankId             = vm.BankId,
                    CardType           = vm.CardType,
                    CardLastFourDigits = vm.CardLastFourDigits,
                    ChequeDate         = vm.ChequeDate,
                    IsAdvancePayment   = false,
                    IsRefund           = false,
                    DiscountAmount     = discShare,
                    RoundOffAmount     = roShare,
                    Status             = "Captured",
                    Remarks            = vm.Remarks,
                    BillingHead        = code,
                    CreatedBy          = CurrentUserId
                };

                await _bookingRepo.AddPaymentAsync(payment);
            }

            var msg = $"Payment of ₹{netTotal:N2} recorded successfully.";
            if (discountAmount > 0)
                msg += discountPercent.HasValue
                    ? $" (Discount: {discountPercent:0.##}% = ₹{discountAmount:N2})"
                    : $" (Discount: ₹{discountAmount:N2})";
            TempData["SuccessMessage"] = msg;
            return RedirectToAction(nameof(Details), new { id = vm.BanquetBookingId });
        }

        // ── Status Actions ────────────────────────────────────────────────────

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            await ChangeStatus(id, "Confirmed", "Event Booking Confirmed");
            TempData["SuccessMessage"] = "Booking confirmed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCheckedIn(int id)
        {
            await ChangeStatus(id, "CheckedIn", "Event started / venue checked in");
            TempData["SuccessMessage"] = "Marked as Checked In.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkEventComplete(int id)
        {
            await ChangeStatus(id, "EventComplete", "Event marked as complete");
            TempData["SuccessMessage"] = "Event marked as complete.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id)
        {
            await _bookingRepo.RecalculateBalanceAsync(id);
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status != "EventComplete" && booking.Status != "CheckedOut")
            {
                TempData["ErrorMessage"] = "Check-out is only allowed for events that have been marked as complete.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (booking.BalanceAmount > 0)
            {
                TempData["WarningMessage"] = $"Balance of ₹{booking.BalanceAmount:N2} is due. Please collect full payment before check-out.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Generate and persist invoice number (idempotent — only if not already set)
            if (string.IsNullOrWhiteSpace(booking.InvoiceNumber))
            {
                var invoiceNumber = await _bookingRepo.GenerateInvoiceNumberAsync();
                await _bookingRepo.SetInvoiceNumberAsync(id, invoiceNumber);
            }

            await ChangeStatus(id, "CheckedOut", "Guest checked out — event fully settled");
            TempData["SuccessMessage"] = "Check-out completed. Event is fully settled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Invoice Print ─────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null || booking.BranchID != CurrentBranchID)
                return NotFound();

            var hotelSettings = await _hotelSettingsRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.HotelSettings = hotelSettings;

            return View(booking);
        }

        private async Task ChangeStatus(int bookingId, string newStatus, string description)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null) return;
            var oldStatus = booking.Status;
            await _bookingRepo.UpdateStatusAsync(bookingId, newStatus, CurrentUserId ?? 0);
            await _bookingRepo.AddAuditLogAsync(new BanquetBookingAuditLog
            {
                BanquetBookingId     = bookingId,
                BanquetBookingNumber = booking.BanquetBookingNumber,
                ActionType           = "StatusChanged",
                ActionDescription    = description,
                OldValue             = oldStatus,
                NewValue             = newStatus,
                PerformedBy          = CurrentUserId
            });
        }

        // ── Cancellation ──────────────────────────────────────────────────────

        public async Task<IActionResult> CancelBooking(int id)
        {
            var preview = await _cancellationRepo.GetPreviewAsync(id);
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null) return NotFound();

            var vm = new BanquetCancelViewModel
            {
                BanquetBookingId     = id,
                BanquetBookingNumber = preview.BanquetBookingNumber,
                EventName            = booking.EventName,
                EventDate            = booking.EventDate,
                TotalAmount          = booking.TotalAmount,
                AmountPaid           = preview.AmountPaid,
                RefundPercent        = preview.RefundPercent,
                FlatDeduction        = preview.FlatDeduction,
                DeductionAmount      = preview.DeductionAmount,
                RefundAmount         = preview.RefundAmount,
                PolicyName           = preview.PolicyName,
                DaysBeforeEvent      = preview.DaysBeforeEvent
            };
            ViewData["Title"] = "Cancel Booking";
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(BanquetCancelViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            await _cancellationRepo.CancelAsync(vm.BanquetBookingId, vm.CancellationReason, vm.AdditionalFlatDeduction, CurrentUserId ?? 0);
            TempData["SuccessMessage"] = "Booking cancelled. Refund process initiated.";
            return RedirectToAction(nameof(Details), new { id = vm.BanquetBookingId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(int cancellationId, int bookingId, string paymentMethod, string? reference)
        {
            if (cancellationId <= 0 || string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["ErrorMessage"] = "Invalid refund details.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }
            // Generate refund number using the proven receipt counter (same as AddPayment)
            var refundNumber = (await _bookingRepo.GenerateNextReceiptNumberAsync(CurrentBranchID))
                                   .Replace("BNQ-RCP", "BNQ-REF");
            var result = await _cancellationRepo.ProcessRefundAsync(cancellationId, paymentMethod, reference ?? string.Empty, CurrentUserId ?? 0, refundNumber);
            if (result != null)
                TempData["SuccessMessage"] = $"Refund {result} processed successfully.";
            else
                TempData["ErrorMessage"] = "Failed to process refund. Please try again.";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }

        // ── Availability Calendar ─────────────────────────────────────────────

        public async Task<IActionResult> AvailabilityCalendar()
        {
            var venues = await _venueRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.Venues = venues;
            ViewData["Title"] = "Availability Calendar";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(string fromDate, string toDate, int? venueId)
        {
            if (!DateOnly.TryParse(fromDate, out var from)) from = DateOnly.FromDateTime(DateTime.Today);
            if (!DateOnly.TryParse(toDate,   out var to))   to   = from.AddMonths(1);

            var events = await _bookingRepo.GetCalendarEventsAsync(CurrentBranchID, from, to, venueId);
            var result = events.Select(e => new
            {
                id    = e.Id,
                title = $"{e.EventName} ({e.VenueName})",
                start = e.EventDate.ToString("yyyy-MM-dd") + (e.EventStartTime.HasValue ? "T" + e.EventStartTime.Value.ToString("HH:mm") : ""),
                end   = (e.EventEndDate ?? e.EventDate).ToString("yyyy-MM-dd") + (e.EventEndTime.HasValue ? "T" + e.EventEndTime.Value.ToString("HH:mm") : ""),
                color = StatusColor(e.Status),
                extendedProps = new {
                    e.AttendeeCount, e.GuaranteePax, e.MealType, e.VenueHireType,
                    e.Status, e.BanquetBookingNumber, e.CustomerType,
                    venueName    = e.VenueName,
                    eventType    = e.EventTypeName,
                    guestName    = e.GuestName,
                    guestPhone   = e.GuestPhone,
                    totalAmount  = e.TotalAmount,
                    balanceAmount= e.BalanceAmount,
                    depositAmount= e.DepositAmount,
                    startTime    = e.EventStartTime.HasValue ? e.EventStartTime.Value.ToString("hh:mm tt") : null,
                    endTime      = e.EventEndTime.HasValue   ? e.EventEndTime.Value.ToString("hh:mm tt")   : null,
                    eventDate    = e.EventDate.ToString("dd MMM yyyy"),
                    eventEndDate = e.EventEndDate.HasValue ? e.EventEndDate.Value.ToString("dd MMM yyyy") : null
                }
            });
            return Json(result);
        }

        // ── AJAX helpers ──────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetVenueQuote(int venueId, string hireType, bool isInterState)
        {
            var venue = await _venueRepo.GetByIdAsync(venueId);
            if (venue == null) return NotFound();
            var rate = hireType == "HalfDay" ? venue.BaseRatePerHalfDay : venue.BaseRatePerDay;
            var gst  = _gstService.CalculateVenueGST(venue, rate, isInterState);
            return Json(new
            {
                venueId = venue.Id, venueName = venue.VenueName, rate,
                gst.GSTPercent, gst.CGSTPercent, gst.SGSTPercent, gst.IGSTPercent,
                gst.GSTAmount, gst.CGSTAmount, gst.SGSTAmount, gst.IGSTAmount,
                gst.TotalAmount, venue.SACCode
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetClientAgreements(int clientId)
        {
            var agreements = await _b2bAgreementRepo.GetByBranchAsync(clientId);
            return Json(agreements.Select(a => new { a.Id, a.AgreementCode, a.AgreementName, a.CreditDays, a.BillingType }));
        }

        [HttpGet]
        public async Task<IActionResult> LookupGuestByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length != 10)
                return Json(null);
            var g = await _bookingRepo.GetLastB2CGuestByPhoneAsync(phone.Trim(), CurrentBranchID);
            if (g == null) return Json(null);
            return Json(new { g.GuestName, g.GuestEmail, g.GuestAddress, g.GuestGSTIN });
        }

        [HttpGet]
        public async Task<IActionResult> GetAddonQuote(int addonId, decimal qty, bool isInterState)
        {
            var addon = await _addonRepo.GetByIdAsync(addonId);
            if (addon == null) return NotFound();
            var baseAmt = addon.Rate * qty;
            var gst     = _gstService.CalculateAddonGST(addon, baseAmt, isInterState);
            return Json(new
            {
                addon.Id, addon.ServiceName, addon.Rate, addon.RateType,
                gst.GSTPercent, gst.CGSTPercent, gst.SGSTPercent, gst.IGSTPercent,
                gst.BaseAmount, gst.GSTAmount, gst.CGSTAmount, gst.SGSTAmount, gst.IGSTAmount, gst.TotalAmount,
                addon.SACCode
            });
        }

        // ── Edit Package (GET — quote for selected package + pax) ─────────────
        [HttpGet]
        public async Task<IActionResult> GetPackageQuote(int packageId, int pax, bool isInterState)
        {
            var pkg = await _packageRepo.GetByIdAsync(packageId);
            if (pkg == null) return NotFound();
            var baseAmt = pkg.PricePerPax * pax;
            var gst     = _gstService.CalculatePackageGST(pkg, baseAmt, isInterState);
            return Json(new
            {
                pkg.Id, pkg.PackageName, pkg.PackageType, pkg.PricePerPax, pkg.SACCode,
                gst.GSTPercent, gst.CGSTPercent, gst.SGSTPercent, gst.IGSTPercent,
                gst.BaseAmount, gst.GSTAmount, gst.CGSTAmount, gst.SGSTAmount, gst.IGSTAmount, gst.TotalAmount
            });
        }

        // ── Edit Package (POST) ───────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPackage(int bookingId, int? packageId, int pax)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null) return NotFound();

            if (booking.Status is "EventComplete" or "CheckedOut" or "Cancelled")
            {
                TempData["ErrorMessage"] = "Package cannot be changed after the event is completed or cancelled.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            // Build old summary for audit
            var oldSummary = booking.PackageLines.Any()
                ? $"{booking.PackageLines[0].PackageName} × {booking.PackageLines[0].Pax} pax @ ₹{booking.PackageLines[0].PricePerPax:0.00}"
                : "No package";

            BanquetBookingPackageLine? newLine = null;
            if (packageId.HasValue && pax > 0)
            {
                var pkg = await _packageRepo.GetByIdAsync(packageId.Value);
                if (pkg == null)
                {
                    TempData["ErrorMessage"] = "Selected package not found.";
                    return RedirectToAction(nameof(Details), new { id = bookingId });
                }

                var baseAmt = pkg.PricePerPax * pax;
                var gst     = _gstService.CalculatePackageGST(pkg, baseAmt, booking.IsInterState);

                newLine = new BanquetBookingPackageLine
                {
                    PackageId       = pkg.Id,
                    PackageName     = pkg.PackageName,
                    PackageType     = pkg.PackageType,
                    PricePerPax     = pkg.PricePerPax,
                    Pax             = pax,
                    BaseAmount      = gst.BaseAmount,
                    GSTPercent      = gst.GSTPercent,
                    CGSTPercent     = gst.CGSTPercent,
                    SGSTPercent     = gst.SGSTPercent,
                    IGSTPercent     = gst.IGSTPercent,
                    GSTAmount       = gst.GSTAmount,
                    CGSTAmount      = gst.CGSTAmount,
                    SGSTAmount      = gst.SGSTAmount,
                    IGSTAmount      = gst.IGSTAmount,
                    TotalAmount     = gst.TotalAmount,
                    MenuDescription = pkg.MenuDescription,
                    SACCode         = pkg.SACCode
                };
            }

            await _bookingRepo.UpdatePackageAsync(bookingId, packageId, newLine, CurrentUserId ?? 0, oldSummary);
            TempData["SuccessMessage"] = packageId.HasValue
                ? $"Package updated successfully. Total recalculated."
                : "Package removed successfully.";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }

        // ── Add Addon (POST) ──────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddon(int bookingId, int addonServiceId, decimal qty, string? notes)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null) return NotFound();

            if (booking.Status is "EventComplete" or "CheckedOut" or "Cancelled")
            {
                TempData["ErrorMessage"] = "Addon services cannot be added after the event is completed or cancelled.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            if (qty <= 0) qty = 1;

            var addon = await _addonRepo.GetByIdAsync(addonServiceId);
            if (addon == null)
            {
                TempData["ErrorMessage"] = "Selected addon service not found.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            // Prevent exact duplicates
            if (booking.AddonLines.Any(a => a.AddonServiceId == addonServiceId))
            {
                TempData["ErrorMessage"] = $"'{addon.ServiceName}' is already added to this booking. Remove it first or adjust quantity.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            var effectiveQty = addon.RateType == "PerPax" ? qty * booking.AttendeeCount : qty;
            var baseAmt      = addon.Rate * effectiveQty;
            var gst          = _gstService.CalculateAddonGST(addon, baseAmt, booking.IsInterState);

            var line = new BanquetBookingAddonLine
            {
                AddonServiceId = addon.Id,
                ServiceName    = addon.ServiceName,
                ServiceType    = addon.ServiceType,
                Rate           = addon.Rate,
                RateType       = addon.RateType,
                Qty            = effectiveQty,
                BaseAmount     = gst.BaseAmount,
                GSTPercent     = gst.GSTPercent,
                CGSTPercent    = gst.CGSTPercent,
                SGSTPercent    = gst.SGSTPercent,
                IGSTPercent    = gst.IGSTPercent,
                GSTAmount      = gst.GSTAmount,
                CGSTAmount     = gst.CGSTAmount,
                SGSTAmount     = gst.SGSTAmount,
                IGSTAmount     = gst.IGSTAmount,
                TotalAmount    = gst.TotalAmount,
                Notes          = notes,
                SACCode        = addon.SACCode
            };

            await _bookingRepo.AddAddonLineAsync(bookingId, line, CurrentUserId ?? 0);
            TempData["SuccessMessage"] = $"'{addon.ServiceName}' added successfully. Total recalculated.";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }

        // ── Remove Addon (POST) ───────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAddon(int bookingId, int addonLineId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null) return NotFound();

            if (booking.Status is "EventComplete" or "CheckedOut" or "Cancelled")
            {
                TempData["ErrorMessage"] = "Addon services cannot be removed after the event is completed or cancelled.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            await _bookingRepo.RemoveAddonLineAsync(bookingId, addonLineId, CurrentUserId ?? 0);
            TempData["SuccessMessage"] = "Addon service removed. Total recalculated.";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task PopulateCreateViewBagAsync()
        {
            ViewBag.Venues      = await _venueRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.EventTypes  = await _eventTypeRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.Packages    = await _packageRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.Addons      = await _addonRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.B2BClients  = await _b2bClientRepo.GetActiveByBranchAsync(CurrentBranchID);
            ViewBag.Banks       = await _bankRepo.GetAllActiveAsync();
            ViewBag.CancellationPolicies = await _cancellationPolicyRepo.GetByBranchAsync(CurrentBranchID);
        }

        private static string StatusColor(string status) => status switch
        {
            "Confirmed"     => "#2196F3",
            "Tentative"     => "#FF9800",
            "Inquiry"       => "#9E9E9E",
            "CheckedIn"     => "#4CAF50",
            "EventComplete" => "#009688",
            "CheckedOut"    => "#3F51B5",
            "Cancelled"     => "#F44336",
            "NoShow"        => "#795548",
            _               => "#607D8B"
        };
    }
}
