using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    public class B2BClient
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Company Code")]
        public string ClientCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        [Display(Name = "Company Type")]
        public string CompanyType { get; set; } = string.Empty;

        [Display(Name = "Agreement Master")]
        public int? AgreementId { get; set; }

        public string? AgreementCode { get; set; }
        public string? AgreementName { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "PAN")]
        public string Pan { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        [Display(Name = "Contact Person Name")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Contact Mobile")]
        public string ContactNo { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Contact Email")]
        public string CorporateEmail { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Alternate Contact")]
        public string? AlternateContact { get; set; }

        [Required]
        [StringLength(250)]
        [Display(Name = "Address Line 1")]
        public string Address { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Country")]
        public int CountryId { get; set; }

        public string? CountryName { get; set; }

        [Required]
        [StringLength(20)]
        public string Pincode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "State")]
        public int StateId { get; set; }

        public string? StateName { get; set; }
        public string? StateCode { get; set; }

        [Display(Name = "Credit Allowed")]
        public bool IsCreditAllowed { get; set; }

        [Range(0, 999999999.99)]
        [Display(Name = "Credit Limit")]
        public decimal? CreditAmount { get; set; }

        [Range(0, 365)]
        [Display(Name = "Credit Days")]
        public int CreditDays { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Billing Cycle")]
        public string BillingCycle { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Billing Type")]
        public string BillingType { get; set; } = string.Empty;

        [Range(0, 999999999.99)]
        [Display(Name = "Outstanding Amount")]
        public decimal OutstandingAmount { get; set; }

        [Display(Name = "Allow Exceed Limit")]
        public bool AllowExceedLimit { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "GSTIN")]
        public string GstNo { get; set; } = string.Empty;

        [StringLength(21)]
        [Display(Name = "CIN")]
        public string? Cin { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "GST Registration Type")]
        public string GstRegistrationType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Place Of Supply")]
        public int PlaceOfSupplyStateId { get; set; }

        public string? PlaceOfSupplyStateName { get; set; }

        [Display(Name = "Reverse Charge")]
        public bool ReverseCharge { get; set; }

        [Display(Name = "e-Invoice Applicable")]
        public bool EInvoiceApplicable { get; set; }

        [Display(Name = "TDS Applicable")]
        public bool TdsApplicable { get; set; }

        [Range(0, 100)]
        [Display(Name = "TDS Percentage")]
        public decimal? TdsPercentage { get; set; }

        [Display(Name = "Blacklisted")]
        public bool Blacklisted { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public string BillingAddressDisplay
        {
            get
            {
                var parts = new[] { Address, AddressLine2, City, StateName, Pincode }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!.Trim());

                return string.Join(", ", parts);
            }
        }
    }
}