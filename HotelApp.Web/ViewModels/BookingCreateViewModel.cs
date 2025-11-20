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

        [Required]
        [EmailAddress]
        public string PrimaryGuestEmail { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PrimaryGuestPhone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? LoyaltyId { get; set; }

        [StringLength(1000)]
        public string? SpecialRequests { get; set; }

        [Range(0, 1000000)]
        public decimal DepositAmount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "Cash";

        public decimal? QuotedBaseAmount { get; set; }
        public decimal? QuotedTaxAmount { get; set; }
        public decimal? QuotedGrandTotal { get; set; }
        public string? QuoteMessage { get; set; }
        public string? AssignedRoomNumber { get; set; }
    }
}
