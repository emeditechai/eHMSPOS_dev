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
        public string RateType { get; set; } = "Standard";
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime? ActualCheckInDate { get; set; }
        public DateTime? ActualCheckOutDate { get; set; }
        public int Nights { get; set; }
        public int RoomTypeId { get; set; }
        public int RequiredRooms { get; set; } = 1;
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
        public int BranchID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }

        public int? CancellationPolicyId { get; set; }
        public string? CancellationPolicySnapshot { get; set; }
        public bool CancellationPolicyAccepted { get; set; }
        public DateTime? CancellationPolicyAcceptedAt { get; set; }

        public RoomType? RoomType { get; set; }
        public Room? Room { get; set; }
        public RateMaster? RatePlan { get; set; }
        public List<BookingGuest> Guests { get; set; } = new();
        public List<BookingPayment> Payments { get; set; } = new();
        public List<BookingRoomNight> RoomNights { get; set; } = new();
        public List<ReservationRoomNight> ReservationRoomNights { get; set; } = new();
        public List<BookingRoom> AssignedRooms { get; set; } = new();
    }

    public class BookingGuest
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int GuestId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? GuestType { get; set; }
        public bool IsPrimary { get; set; }
        public string? RelationshipToPrimary { get; set; }
        public int? Age { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? IdentityType { get; set; }
        public string? IdentityNumber { get; set; }
        public string? DocumentPath { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Pincode { get; set; }
        public int? CountryId { get; set; }
        public int? StateId { get; set; }
        public int? CityId { get; set; }
        public string? Gender { get; set; }

        // Optional: captured via webcam on booking creation; persisted on Guests table for primary guest.
        public byte[]? Photo { get; set; }
        public string? PhotoContentType { get; set; }

        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
    }

    public class BookingPayment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? ReceiptGroupNumber { get; set; }
        public string? BillingHead { get; set; }
        public decimal Amount { get; set; }
        // Payment-time adjustments
        public decimal DiscountAmount { get; set; } = 0m;
        public decimal? DiscountPercent { get; set; }
        public decimal RoundOffAmount { get; set; } = 0m;
        public bool IsRoundOffApplied { get; set; } = false;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string Status { get; set; } = "Captured";
        public DateTime PaidOn { get; set; }
        public string? Notes { get; set; }
        public string? CardType { get; set; }
        public string? CardLastFourDigits { get; set; }
        public int? BankId { get; set; }
        public DateTime? ChequeDate { get; set; }
        public bool IsAdvancePayment { get; set; } = false;
        public bool IsRefund { get; set; } = false;
    }

    public class BookingRoomNight
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int? RoomId { get; set; }
        public DateTime StayDate { get; set; }
        public decimal RateAmount { get; set; }
        public decimal ActualBaseRate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public string Status { get; set; } = "Reserved";
    }

    public class ReservationRoomNight
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public DateTime StayDate { get; set; }
        public decimal RateAmount { get; set; }
        public decimal ActualBaseRate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public string Status { get; set; } = "Reserved";
        public DateTime CreatedDate { get; set; }
    }
}
