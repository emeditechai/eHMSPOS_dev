using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HotelApp.Web.Models
{
    public class B2BAgreement
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Agreement Code")]
        public string AgreementCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Agreement Name")]
        public string AgreementName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Contract Reference")]
        public string? ContractReference { get; set; }

        [StringLength(30)]
        [Display(Name = "Agreement Type")]
        public string AgreementType { get; set; } = "Corporate";

        [Required]
        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; }

        [Required]
        [Display(Name = "Effective To")]
        [DataType(DataType.Date)]
        public DateTime EffectiveTo { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Billing Type")]
        public string BillingType { get; set; } = "Credit";

        [Range(0, 365)]
        [Display(Name = "Credit Days")]
        public int CreditDays { get; set; }

        [StringLength(20)]
        [Display(Name = "Billing Cycle")]
        public string? BillingCycle { get; set; }

        [StringLength(250)]
        [Display(Name = "Payment Terms")]
        public string? PaymentTerms { get; set; }

        [Display(Name = "Security Deposit")]
        [Range(0, 999999999)]
        public decimal? SecurityDepositAmount { get; set; }

        [Display(Name = "Credit Limit Override")]
        [Range(0, 999999999)]
        public decimal? CreditLimit { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Rate Plan")]
        public string RatePlanType { get; set; } = "Corporate Rate";

        [Range(0, 100)]
        [Display(Name = "Discount %")]
        public decimal DiscountPercent { get; set; }

        [StringLength(20)]
        [Display(Name = "Meal Plan")]
        public string? MealPlan { get; set; }

        [Display(Name = "Terms & Conditions")]
        public int? TermsConditionId { get; set; }

        public string? TermsConditionName { get; set; }

        [Display(Name = "Cancellation Policy")]
        public int? CancellationPolicyId { get; set; }

        public string? CancellationPolicyName { get; set; }

        [Display(Name = "Default GST Slab")]
        public int? GstSlabId { get; set; }

        public string? GstSlabName { get; set; }

        [StringLength(500)]
        [Display(Name = "Seasonal Rate Notes")]
        public string? SeasonalRateNotes { get; set; }

        [StringLength(500)]
        [Display(Name = "Blackout Notes")]
        public string? BlackoutDatesNotes { get; set; }

        [Display(Name = "Amendments Allowed")]
        public bool IsAmendmentAllowed { get; set; } = true;

        [Display(Name = "Amendment Charge")]
        [Range(0, 999999999)]
        public decimal? AmendmentChargeAmount { get; set; }

        [Display(Name = "Breakfast Included")]
        public bool IncludesBreakfast { get; set; }

        [Display(Name = "Lunch Included")]
        public bool IncludesLunch { get; set; }

        [Display(Name = "Dinner Included")]
        public bool IncludesDinner { get; set; }

        [Display(Name = "Laundry Included")]
        public bool IncludesLaundry { get; set; }

        [Display(Name = "Airport Transfer Included")]
        public bool IncludesAirportTransfer { get; set; }

        [Display(Name = "Wi-Fi Included")]
        public bool IncludesWifi { get; set; }

        [Display(Name = "Lounge Access Included")]
        public bool IncludesAccessToLounge { get; set; }

        [StringLength(500)]
        [Display(Name = "Service Remarks")]
        public string? ServiceRemarks { get; set; }

        [StringLength(30)]
        [Display(Name = "Approval Status")]
        public string ApprovalStatus { get; set; } = "Draft";

        [Display(Name = "Approved By")]
        public int? ApprovedByUserId { get; set; }

        public string? ApprovedByName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Approved Date")]
        public DateTime? ApprovedDate { get; set; }

        [StringLength(120)]
        [Display(Name = "Signed By")]
        public string? SignedByName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Signed Date")]
        public DateTime? SignedDate { get; set; }

        [StringLength(250)]
        [Display(Name = "Signed Document Path")]
        public string? SignedDocumentPath { get; set; }

        [Display(Name = "Signed Document")]
        public IFormFile? SignedDocumentFile { get; set; }

        [Display(Name = "Auto Renew")]
        public bool AutoRenew { get; set; }

        [Range(0, 365)]
        [Display(Name = "Renewal Notice Days")]
        public int? RenewalNoticeDays { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        [StringLength(1000)]
        [Display(Name = "Internal Remarks")]
        public string? InternalRemarks { get; set; }

        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
        public int RoomRateCount { get; set; }
        public int AssignedClientCount { get; set; }
        public IList<B2BAgreementRoomRate> RoomRates { get; set; } = new List<B2BAgreementRoomRate>();
    }

    public class B2BAgreementRoomRate
    {
        public int Id { get; set; }
        public int AgreementId { get; set; }

        [Display(Name = "Room Type")]
        public int RoomTypeId { get; set; }

        public string? RoomTypeName { get; set; }

        [StringLength(50)]
        [Display(Name = "Season")]
        public string? SeasonLabel { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Valid From")]
        public DateTime ValidFrom { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Valid To")]
        public DateTime ValidTo { get; set; }

        [Range(0, 999999999)]
        [Display(Name = "Base Rate")]
        public decimal BaseRate { get; set; }

        [Range(0.01, 999999999)]
        [Display(Name = "Contract Rate")]
        public decimal ContractRate { get; set; }

        [Range(0, 999999999)]
        [Display(Name = "Extra Pax Rate")]
        public decimal ExtraPaxRate { get; set; }

        [StringLength(20)]
        [Display(Name = "Meal Plan")]
        public string? MealPlan { get; set; }

        [Display(Name = "GST Slab")]
        public int? GstSlabId { get; set; }

        public string? GstSlabName { get; set; }

        [StringLength(250)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;
    }
}