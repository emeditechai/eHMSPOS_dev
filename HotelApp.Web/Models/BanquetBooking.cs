using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    // ── BanquetBooking (Header) ───────────────────────────────────────────────

    public class BanquetBooking
    {
        public int Id { get; set; }
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public int BranchID { get; set; }

        // Event Date & Time
        [Required]
        [Display(Name = "Event Date")]
        public DateOnly EventDate { get; set; }

        [Display(Name = "Event End Date")]
        public DateOnly? EventEndDate { get; set; }

        [Display(Name = "Start Time")]
        public TimeOnly? EventStartTime { get; set; }

        [Display(Name = "End Time")]
        public TimeOnly? EventEndTime { get; set; }

        [Display(Name = "Setup Time")]
        public TimeOnly? SetupTime { get; set; }

        [Display(Name = "Teardown Time")]
        public TimeOnly? TeardownTime { get; set; }

        // Venue
        [Required]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }
        public string? VenueName { get; set; }
        public string? VenueType { get; set; }

        // Event Details
        [Required]
        [Display(Name = "Event Type")]
        public int EventTypeId { get; set; }
        public string? EventTypeName { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Expected Attendees")]
        public int AttendeeCount { get; set; }

        [Display(Name = "Guarantee Pax")]
        public int GuaranteePax { get; set; }

        [Display(Name = "Children")]
        public int ChildCount { get; set; }

        [Display(Name = "Meal Type")]
        public string MealType { get; set; } = "Veg";

        // Customer
        [Required]
        [Display(Name = "Customer Type")]
        public string CustomerType { get; set; } = "B2C";

        // B2C
        [Display(Name = "Primary Guest")]
        public int? PrimaryGuestId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Guest / Contact Name")]
        public string GuestName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Phone]
        [Display(Name = "Phone")]
        public string GuestPhone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string? GuestEmail { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? GuestAddress { get; set; }

        [StringLength(15)]
        [Display(Name = "GSTIN (Optional)")]
        public string? GuestGSTIN { get; set; }

        // B2B
        [Display(Name = "B2B Client")]
        public int? B2BClientId { get; set; }
        public string? B2BClientName { get; set; }

        [Display(Name = "Agreement")]
        public int? B2BAgreementId { get; set; }

        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(15)]
        [Display(Name = "Company GSTIN")]
        public string? CompanyGSTIN { get; set; }

        [StringLength(10)]
        [Display(Name = "Company PAN")]
        public string? CompanyPAN { get; set; }

        [StringLength(500)]
        [Display(Name = "Billing Address")]
        public string? CompanyAddress { get; set; }

        [Display(Name = "Bill To")]
        public string BillingTo { get; set; } = "Guest";

        [Display(Name = "Credit Days")]
        public int CreditDays { get; set; }

        [Display(Name = "Inter-State Supply")]
        public bool IsInterState { get; set; }

        // Venue Hire Type
        [Display(Name = "Venue Hire")]
        public string VenueHireType { get; set; } = "FullDay";

        // Package
        [Display(Name = "Package")]
        public int? PackageId { get; set; }
        public string? PackageName { get; set; }
        public decimal PackagePricePerPax { get; set; }
        public int PackageTotalPax { get; set; }

        // Financials – all calculated and stored; never hardcoded
        public decimal PackageBaseAmount { get; set; }
        public decimal PackageGSTAmount { get; set; }
        public decimal PackageCGSTAmount { get; set; }
        public decimal PackageSGSTAmount { get; set; }
        public decimal PackageIGSTAmount { get; set; }

        public decimal VenueBaseAmount { get; set; }
        public decimal VenueGSTAmount { get; set; }
        public decimal VenueCGSTAmount { get; set; }
        public decimal VenueSGSTAmount { get; set; }
        public decimal VenueIGSTAmount { get; set; }

        public decimal AddonBaseAmount { get; set; }
        public decimal AddonGSTAmount { get; set; }
        public decimal AddonCGSTAmount { get; set; }
        public decimal AddonSGSTAmount { get; set; }
        public decimal AddonIGSTAmount { get; set; }

        public decimal TotalBaseAmount { get; set; }
        public decimal TotalGSTAmount { get; set; }
        public decimal TotalCGSTAmount { get; set; }
        public decimal TotalSGSTAmount { get; set; }
        public decimal TotalIGSTAmount { get; set; }
        public decimal ServiceChargeAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal BalanceAmount { get; set; }

        // Status
        public string Status { get; set; } = "Inquiry";
        public string PaymentStatus { get; set; } = "Pending";
        public string ApprovalStatus { get; set; } = "Draft";

        // Cancellation
        public int? CancellationPolicyId { get; set; }
        public string? CancellationPolicySnapshot { get; set; }

        // Linked hotel booking
        public int? LinkedHotelBookingId { get; set; }
        public string? LinkedHotelBookingNumber { get; set; }

        // Invoice
        public string? InvoiceNumber { get; set; }

        // Misc
        [StringLength(2000)]
        [Display(Name = "Special Requests")]
        public string? SpecialRequests { get; set; }

        [StringLength(2000)]
        [Display(Name = "Internal Notes")]
        public string? InternalNotes { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }
        public string? CreatedByName { get; set; }

        // Nested collections (populated on detail load)
        public List<BanquetBookingPackageLine> PackageLines { get; set; } = new();
        public List<BanquetBookingAddonLine> AddonLines { get; set; } = new();
        public List<BanquetBookingPayment> Payments { get; set; } = new();
        public List<BanquetBookingAuditLog> AuditLogs { get; set; } = new();
    }

    // ── BanquetBookingPackageLine ─────────────────────────────────────────────

    public class BanquetBookingPackageLine
    {
        public int Id { get; set; }
        public int BanquetBookingId { get; set; }
        public int? PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public string MealType { get; set; } = "Veg";
        public decimal PricePerPax { get; set; }
        public int Pax { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal CGSTPercent { get; set; }
        public decimal SGSTPercent { get; set; }
        public decimal IGSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal IGSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? MenuDescription { get; set; }
        public string? SACCode { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // ── BanquetBookingAddonLine ───────────────────────────────────────────────

    public class BanquetBookingAddonLine
    {
        public int Id { get; set; }
        public int BanquetBookingId { get; set; }
        public int? AddonServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public string RateType { get; set; } = "PerEvent";
        public decimal Qty { get; set; } = 1;
        public decimal BaseAmount { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal CGSTPercent { get; set; }
        public decimal SGSTPercent { get; set; }
        public decimal IGSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal IGSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public string? SACCode { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // ── BanquetBookingPayment ─────────────────────────────────────────────────

    public class BanquetBookingPayment
    {
        public int Id { get; set; }
        public int BanquetBookingId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";
        public string? PaymentReference { get; set; }
        public string Status { get; set; } = "Captured";
        public DateTime PaidOn { get; set; }
        public int? BankId { get; set; }
        public string? BankName { get; set; }
        public string? CardType { get; set; }
        public string? CardLastFourDigits { get; set; }
        public DateOnly? ChequeDate { get; set; }
        public bool IsAdvancePayment { get; set; }
        public bool IsRefund { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
    }

    // ── BanquetBookingAuditLog ────────────────────────────────────────────────

    public class BanquetBookingAuditLog
    {
        public int Id { get; set; }
        public int BanquetBookingId { get; set; }
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionDescription { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public int? PerformedBy { get; set; }
        public string? PerformedByName { get; set; }
        public DateTime PerformedAt { get; set; }
    }

    // ── BanquetCancellation ───────────────────────────────────────────────────

    public class BanquetCancellation
    {
        public int Id { get; set; }
        public int BanquetBookingId { get; set; }
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal RefundPercent { get; set; }
        public decimal FlatDeduction { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public bool IsRefunded { get; set; }
        public string? RefundPaymentMethod { get; set; }
        public string? RefundReference { get; set; }
        public DateTime? RefundedOn { get; set; }
        public string? RefundNumber { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";
        public string? CancellationReason { get; set; }
        public DateTime CancelledOn { get; set; }
        public int? CancelledBy { get; set; }
    }
}
