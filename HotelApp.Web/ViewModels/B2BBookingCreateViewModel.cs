using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.ViewModels
{
    /// <summary>One room-type line chosen by the user on the Create B2B Booking page.</summary>
    public class B2BRoomLineItem
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public int RequiredRooms { get; set; } = 1;
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int Adults { get; set; } = 1;
        public int Children { get; set; }
        public string? MealPlan { get; set; }
    }

    public class B2BBookingCreateViewModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a B2B client.")]
        [Display(Name = "B2B Client")]
        public int ClientId { get; set; }

        [Required]
        [Display(Name = "Agreement")]
        public int AgreementId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-In")]
        public DateTime CheckInDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-Out")]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Display(Name = "B2B Source")]
        public string Source { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Channel")]
        public string Channel { get; set; } = string.Empty;

        /// <summary>Selected room-type lines (one per room type chosen in the agreement).</summary>
        public List<B2BRoomLineItem> RoomLines { get; set; } = new();

        // Keep for backward-compat / primary room type storage
        public int RoomTypeId { get; set; }

        [Range(1, 50)]
        [Display(Name = "Total Rooms")]
        public int RequiredRooms { get; set; } = 1;

        [Required]
        [Range(1, 10)]
        public int Adults { get; set; } = 1;

        [Range(0, 10)]
        public int Children { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Primary Guest First Name")]
        public string PrimaryGuestFirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Primary Guest Last Name")]
        public string PrimaryGuestLastName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile number must be exactly 10 digits.")]
        [Display(Name = "Primary Guest Mobile")]
        public string PrimaryGuestPhone { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "Primary Guest Email")]
        public string? PrimaryGuestEmail { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Advance Amount")]
        public decimal DepositAmount { get; set; }

        [Display(Name = "Collect Advance Now")]
        public bool CollectAdvancePayment { get; set; }

        [Display(Name = "Advance Payment Method")]
        public string? AdvancePaymentMethod { get; set; }

        [StringLength(100)]
        [Display(Name = "Payment Reference")]
        public string? AdvancePaymentReference { get; set; }

        [StringLength(1000)]
        [Display(Name = "Booking Notes")]
        public string? SpecialRequests { get; set; }

        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string AgreementCode { get; set; } = string.Empty;
        public string AgreementName { get; set; } = string.Empty;
        public string? CompanyContactPerson { get; set; }
        public string? CompanyContactNo { get; set; }
        public string? CompanyEmail { get; set; }
        public string? CompanyGstNo { get; set; }
        public string? BillingAddress { get; set; }
        public string? BillingStateName { get; set; }
        public string? BillingPincode { get; set; }
        public string BillingType { get; set; } = string.Empty;
        public string BillingTo { get; set; } = "Company";
        public int CreditDays { get; set; }
        public string? MealPlan { get; set; }
        public decimal CorporateDiscountPercent { get; set; }
        public decimal CompanyCreditLimit { get; set; }
        public bool IsCreditAllowed { get; set; }
        public int? GstSlabId { get; set; }
        public string? GstSlabCode { get; set; }
        public string? GstSlabName { get; set; }
        public decimal? QuotedBaseAmount { get; set; }
        public decimal? QuotedTaxAmount { get; set; }
        public decimal? QuotedGrandTotal { get; set; }
        public string? QuoteMessage { get; set; }
    }
}