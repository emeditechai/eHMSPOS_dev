using System.ComponentModel.DataAnnotations;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels
{
    // ── Booking Create ────────────────────────────────────────────────────────

    public class BanquetBookingCreateViewModel
    {
        // -- Step 1: Customer
        [Required]
        public string CustomerType { get; set; } = "B2C";

        // B2C
        public int? PrimaryGuestId { get; set; }

        [Required(ErrorMessage = "Contact name is required.")]
        [StringLength(200)]
        [Display(Name = "Contact Name")]
        public string GuestName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone]
        [StringLength(20)]
        public string GuestPhone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? GuestEmail { get; set; }

        [StringLength(500)]
        public string? GuestAddress { get; set; }

        [StringLength(15)]
        [Display(Name = "GSTIN (Optional)")]
        public string? GuestGSTIN { get; set; }

        // B2B
        public int? B2BClientId { get; set; }
        public int? B2BAgreementId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyGSTIN { get; set; }
        public string? CompanyPAN { get; set; }
        public string? CompanyAddress { get; set; }
        public string BillingTo { get; set; } = "Guest";
        public int CreditDays { get; set; }
        public bool IsInterState { get; set; }

        // -- Step 2: Venue & Event
        [Required(ErrorMessage = "Venue is required.")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Event date is required.")]
        [Display(Name = "Event Date")]
        public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        [Display(Name = "Event End Date")]
        public DateOnly? EventEndDate { get; set; }

        [Display(Name = "Start Time")]
        public string? EventStartTime { get; set; }

        [Display(Name = "End Time")]
        public string? EventEndTime { get; set; }

        [Display(Name = "Setup Time")]
        public string? SetupTime { get; set; }

        [Display(Name = "Teardown Time")]
        public string? TeardownTime { get; set; }

        [Required(ErrorMessage = "Event type is required.")]
        public int EventTypeId { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(200)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Expected Attendees")]
        [Range(1, 100000)]
        public int AttendeeCount { get; set; } = 50;

        [Display(Name = "Guarantee Pax")]
        public int GuaranteePax { get; set; }

        [Display(Name = "Children")]
        public int ChildCount { get; set; }

        [Display(Name = "Meal Type")]
        public string MealType { get; set; } = "Veg";

        [Display(Name = "Venue Hire")]
        public string VenueHireType { get; set; } = "FullDay";

        // -- Step 3: Package
        public int? PackageId { get; set; }
        public int PackageTotalPax { get; set; }

        // -- Step 4: Addons (JSON from form)
        public string? AddonLinesJson { get; set; }

        // -- Step 5: Financial overrides
        public decimal ServiceChargeAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool ApplyRoundOff { get; set; }

        // -- Step 6: Advance Payment
        public decimal AdvancePaymentAmount { get; set; }
        public string AdvancePaymentMethod { get; set; } = "Cash";
        public string? AdvancePaymentReference { get; set; }
        public int? AdvanceBankId { get; set; }

        // -- Misc
        public int? CancellationPolicyId { get; set; }
        public string? SpecialRequests { get; set; }
        public string? InternalNotes { get; set; }
        public int? LinkedHotelBookingId { get; set; }
    }

    // ── Add Payment ───────────────────────────────────────────────────────────

    public class BanquetAddPaymentViewModel
    {
        public int BanquetBookingId { get; set; }
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal BalanceAmount { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        [Display(Name = "Reference / Transaction ID")]
        public string? PaymentReference { get; set; }

        public int? BankId { get; set; }
        public string? CardType { get; set; }
        public string? CardLastFourDigits { get; set; }
        public DateOnly? ChequeDate { get; set; }
        /// <summary>None | Percent | Flat</summary>
        public string DiscountType { get; set; } = "None";
        public decimal? DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public bool IsRefund { get; set; }
        public string? Remarks { get; set; }

        // ── Head-wise due amounts (passed from controller GET) ─────────────
        public decimal VenueTotal { get; set; }
        public decimal VenueDue { get; set; }
        public decimal PackageTotal { get; set; }
        public decimal PackageDue { get; set; }
        public decimal AddonTotal { get; set; }
        public decimal AddonDue { get; set; }

        // ── Head-wise allocation (posted from form as JSON) ───────────────
        public string? HeadAllocations { get; set; }
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public class BanquetCancelViewModel
    {
        public int BanquetBookingId { get; set; }
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RefundPercent { get; set; }
        public decimal FlatDeduction { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public int DaysBeforeEvent { get; set; }

        [Required]
        public string CancellationReason { get; set; } = string.Empty;

        public decimal AdditionalFlatDeduction { get; set; }
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public class BanquetDashboardViewModel
    {
        public int TodaysEvents { get; set; }
        public int UpcomingEvents7Days { get; set; }
        public int PendingConfirmations { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int ThisMonthBookings { get; set; }
        public List<BanquetBooking> TodaysEventList { get; set; } = new();
        public List<BanquetBooking> RecentBookings { get; set; } = new();
    }

    // ── List / Filter ─────────────────────────────────────────────────────────

    public class BanquetBookingListViewModel
    {
        public IEnumerable<BanquetBooking> Bookings { get; set; } = new List<BanquetBooking>();
        public string? FilterStatus { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int? FilterVenueId { get; set; }
        public string? FilterCustomerType { get; set; }
        public IEnumerable<BanquetVenue> Venues { get; set; } = new List<BanquetVenue>();
    }

    // ── Addon line (used in create form JSON deserialization) ─────────────────

    public class BanquetAddonLineInput
    {
        public int AddonServiceId { get; set; }
        public decimal Qty { get; set; } = 1;
        public string? Notes { get; set; }
    }

    // ── Report ViewModels ─────────────────────────────────────────────────────

    public class BanquetCollectionRegisterViewModel
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public BanquetCollectionSummaryRow Summary { get; set; } = new();
        public IEnumerable<BanquetCollectionDailyRow> DailyTotals { get; set; } = new List<BanquetCollectionDailyRow>();
        public IEnumerable<BanquetCollectionDetailRow> Details { get; set; } = new List<BanquetCollectionDetailRow>();
    }

    public class BanquetGSTRegisterViewModel
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public IEnumerable<BanquetGSTLineRow> Rows { get; set; } = new List<BanquetGSTLineRow>();
        public decimal TotalTaxableValue => Rows.Sum(r => r.TaxableValue);
        public decimal TotalCGST => Rows.Sum(r => r.CGST);
        public decimal TotalSGST => Rows.Sum(r => r.SGST);
        public decimal TotalIGST => Rows.Sum(r => r.IGST);
        public decimal TotalGST => Rows.Sum(r => r.TotalGST);
    }

    public class BanquetVenueUtilizationViewModel
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public IEnumerable<BanquetVenueUtilizationRow> Rows { get; set; } = new List<BanquetVenueUtilizationRow>();
    }

    public class BanquetEventTypePerformanceViewModel
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public IEnumerable<BanquetEventTypePerformanceRow> Rows { get; set; } = new List<BanquetEventTypePerformanceRow>();
    }

    public class BanquetOutstandingViewModel
    {
        public IEnumerable<BanquetOutstandingRow> Rows { get; set; } = new List<BanquetOutstandingRow>();
        public decimal TotalOutstanding => Rows.Sum(r => r.BalanceAmount);
    }
}

