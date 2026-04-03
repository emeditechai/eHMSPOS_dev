using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    public class B2BTermsCondition
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Terms Code")]
        public string TermsCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Terms Title")]
        public string TermsTitle { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Terms Type")]
        public string TermsType { get; set; } = "General";

        [Display(Name = "Cancellation Policy")]
        public int? CancellationPolicyId { get; set; }

        public string? CancellationPolicyName { get; set; }

        [StringLength(500)]
        [Display(Name = "Payment Terms")]
        public string? PaymentTerms { get; set; }

        [StringLength(1000)]
        [Display(Name = "Refund Policy")]
        public string? RefundPolicy { get; set; }

        [StringLength(1000)]
        [Display(Name = "No Show Policy")]
        public string? NoShowPolicy { get; set; }

        [StringLength(1000)]
        [Display(Name = "Amendment Policy")]
        public string? AmendmentPolicy { get; set; }

        [StringLength(1000)]
        [Display(Name = "Check-In / Check-Out Policy")]
        public string? CheckInCheckOutPolicy { get; set; }

        [StringLength(1000)]
        [Display(Name = "Child Policy")]
        public string? ChildPolicy { get; set; }

        [StringLength(1000)]
        [Display(Name = "Extra Bed Policy")]
        public string? ExtraBedPolicy { get; set; }

        [StringLength(1000)]
        [Display(Name = "Billing Instructions")]
        public string? BillingInstructions { get; set; }

        [StringLength(1000)]
        [Display(Name = "Tax Notes")]
        public string? TaxNotes { get; set; }

        [StringLength(2000)]
        [Display(Name = "Legal Disclaimer")]
        public string? LegalDisclaimer { get; set; }

        [StringLength(2000)]
        [Display(Name = "Special Conditions")]
        public string? SpecialConditions { get; set; }

        [Display(Name = "Default Terms")]
        public bool IsDefault { get; set; }

        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
    }
}