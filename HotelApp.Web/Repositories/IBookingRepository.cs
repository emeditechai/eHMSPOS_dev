using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IBookingRepository
    {
        Task<BookingQuoteResult?> GetQuoteAsync(BookingQuoteRequest request);
        Task<Room?> FindAvailableRoomAsync(int roomTypeId, DateTime checkIn, DateTime checkOut);
        Task<BookingCreationResult> CreateBookingAsync(Booking booking, IEnumerable<BookingGuest> guests, IEnumerable<BookingPayment> payments, IEnumerable<BookingRoomNight> roomNights);
        Task<IEnumerable<Booking>> GetRecentAsync(int take = 25);
        Task<Booking?> GetByBookingNumberAsync(string bookingNumber);
        Task<int> GetTodayBookingCountAsync();
        Task<decimal> GetTodayAdvanceAmountAsync();
        Task<int> GetTodayCheckInCountAsync();
        Task<bool> UpdateRoomAssignmentAsync(string bookingNumber, int roomId);
        Task<bool> UpdateBookingDatesAsync(string bookingNumber, DateTime checkInDate, DateTime checkOutDate, int nights, decimal baseAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount, decimal totalAmount);
        Task<IEnumerable<BookingAuditLog>> GetAuditLogAsync(int bookingId);
        Task AddAuditLogAsync(int bookingId, string bookingNumber, string actionType, string description, string? oldValue = null, string? newValue = null, int? performedBy = null);
    }
}
