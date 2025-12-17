using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.ViewModels
{
    public class BookingCreateViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Range(1, 10)]
        public int Adults { get; set; } = 1;

        [Range(0, 10)]
        public int Children { get; set; }

        [Required]
        public int RoomTypeId { get; set; }

        [Required]
        [Range(1, 50, ErrorMessage = "Required Rooms must be between 1 and 50")]
        [Display(Name = "Required Rooms")]
        public int RequiredRooms { get; set; } = 1;

        [Required]
        public string CustomerType { get; set; } = string.Empty;

        [Required]
        public string Source { get; set; } = string.Empty;

        [Required]
        public string Channel { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PrimaryGuestFirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PrimaryGuestLastName { get; set; } = string.Empty;

        [EmailAddress]
        public string? PrimaryGuestEmail { get; set; }

    [Required]
    [Phone]
    public string PrimaryGuestPhone { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Range(0, 120)]
        public int? Age { get; set; }

    [StringLength(100)]
    public string? LoyaltyId { get; set; }

        [StringLength(1000)]
        public string? SpecialRequests { get; set; }

        [Range(0, 1000000)]
        public decimal DepositAmount { get; set; }

        [Display(Name = "Collect Advance Payment Now")]
        public bool CollectAdvancePayment { get; set; }

        [Display(Name = "Country")]
        public int? CountryId { get; set; }

        [Display(Name = "State")]
        public int? StateId { get; set; }

        [Display(Name = "City")]
        public int? CityId { get; set; }

        [StringLength(250)]
        [Display(Name = "Address")]
        public string? AddressLine { get; set; }

        [StringLength(20)]
        public string? Pincode { get; set; }

        public decimal? QuotedBaseAmount { get; set; }
        public decimal? QuotedTaxAmount { get; set; }
        public decimal? QuotedGrandTotal { get; set; }
        public string? QuoteMessage { get; set; }
        public string? AssignedRoomNumber { get; set; }
    }
}
