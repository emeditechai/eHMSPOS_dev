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

            // Advance payment
            BanquetBookingPayment? advance = null;
            if (vm.AdvancePaymentAmount > 0)
            {
                var receiptNum = await _bookingRepo.GenerateNextReceiptNumberAsync(CurrentBranchID);
                advance = new BanquetBookingPayment
                {
                    ReceiptNumber    = receiptNum,
                    Amount           = vm.AdvancePaymentAmount,
                    PaymentMethod    = vm.AdvancePaymentMethod,
                    PaymentReference = vm.AdvancePaymentReference,
                    BankId           = vm.AdvanceBankId,
                    Status           = "Captured",
                    IsAdvancePayment = true,
                    CreatedBy        = CurrentUserId
                };
                booking.DepositAmount = vm.AdvancePaymentAmount;
                booking.BalanceAmount = booking.TotalAmount - vm.AdvancePaymentAmount;
                booking.PaymentStatus = vm.AdvancePaymentAmount >= booking.TotalAmount ? "FullPaid" : "PartialPaid";
            }

            var bookingId = await _bookingRepo.CreateAsync(booking, packageLines, addonLines, advance);
            TempData["SuccessMessage"] = $"Banquet booking {bookingNumber} created successfully!";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }

        // ── Details ───────────────────────────────────────────────────────────

        public async Task<IActionResult> Details(int id)
        {
            await _bookingRepo.RecalculateBalanceAsync(id);
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null) return NotFound();            if (booking.Status == "Cancelled")
                ViewBag.Cancellation = await _cancellationRepo.GetByBookingIdAsync(id);            ViewData["Title"] = $"Booking – {booking.BanquetBookingNumber}";
            return View(booking);
        }

        // ── Add Payment ───────────────────────────────────────────────────────

        public async Task<IActionResult> AddPayment(int id)
        {
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null) return NotFound();

            var vm = new BanquetAddPaymentViewModel
            {
                BanquetBookingId     = booking.Id,
                BanquetBookingNumber = booking.BanquetBookingNumber,
                EventName            = booking.EventName,
                TotalAmount          = booking.TotalAmount,
                BalanceAmount        = booking.BalanceAmount,
                Amount               = booking.BalanceAmount
            };
            ViewBag.Banks = await _bankRepo.GetAllActiveAsync();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(BanquetAddPaymentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Banks = await _bankRepo.GetAllActiveAsync();
                return View(vm);
            }

            // Compute discount amount from type
            var grossAmount  = vm.Amount;
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

            var netAmount = Math.Round(grossAmount - discountAmount + vm.RoundOffAmount, 2, MidpointRounding.AwayFromZero);
            if (netAmount < 0)
            {
                ModelState.AddModelError(string.Empty, "Net payable amount cannot be negative.");
                ViewBag.Banks = await _bankRepo.GetAllActiveAsync();
                return View(vm);
            }

            var receiptNum = await _bookingRepo.GenerateNextReceiptNumberAsync(CurrentBranchID);
            var payment = new BanquetBookingPayment
            {
                BanquetBookingId   = vm.BanquetBookingId,
                ReceiptNumber      = receiptNum,
                Amount             = netAmount,
                PaymentMethod      = vm.PaymentMethod,
                PaymentReference   = vm.PaymentReference,
                BankId             = vm.BankId,
                CardType           = vm.CardType,
                CardLastFourDigits = vm.CardLastFourDigits,
                ChequeDate         = vm.ChequeDate,
                IsAdvancePayment   = false,
                IsRefund           = vm.IsRefund,
                DiscountAmount     = discountAmount,
                RoundOffAmount     = vm.RoundOffAmount,
                Status             = "Captured",
                Remarks            = vm.Remarks,
                CreatedBy          = CurrentUserId
            };

            await _bookingRepo.AddPaymentAsync(payment);
            var msg = $"Payment of ₹{netAmount:N2} recorded successfully.";
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
                return RedirectToAction(nameof(AddPayment), new { id });
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
