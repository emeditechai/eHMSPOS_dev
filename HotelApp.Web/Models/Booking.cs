using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Pending";
        public string Channel { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Nights { get; set; }
        public int RoomTypeId { get; set; }
        public int? RoomId { get; set; }
        public int? RatePlanId { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public string PrimaryGuestFirstName { get; set; } = string.Empty;
        public string PrimaryGuestLastName { get; set; } = string.Empty;
        public string PrimaryGuestEmail { get; set; } = string.Empty;
        public string PrimaryGuestPhone { get; set; } = string.Empty;
        public string? LoyaltyId { get; set; }
        public string? SpecialRequests { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }

        public RoomType? RoomType { get; set; }
        public Room? Room { get; set; }
        public RateMaster? RatePlan { get; set; }
        public List<BookingGuest> Guests { get; set; } = new();
        public List<BookingPayment> Payments { get; set; } = new();
        public List<BookingRoomNight> RoomNights { get; set; } = new();
    }

    public class BookingGuest
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? GuestType { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class BookingPayment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string Status { get; set; } = "Captured";
        public DateTime PaidOn { get; set; }
        public string? Notes { get; set; }
    }

    public class BookingRoomNight
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int? RoomId { get; set; }
        public DateTime StayDate { get; set; }
        public decimal RateAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public string Status { get; set; } = "Reserved";
    }
}
