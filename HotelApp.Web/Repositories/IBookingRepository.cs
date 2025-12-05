using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IBookingRepository
    {
        Task<BookingQuoteResult?> GetQuoteAsync(BookingQuoteRequest request);
        Task<Room?> FindAvailableRoomAsync(int roomTypeId, DateTime checkIn, DateTime checkOut);
        Task<BookingCreationResult> CreateBookingAsync(Booking booking, IEnumerable<BookingGuest> guests, IEnumerable<BookingPayment> payments, IEnumerable<BookingRoomNight> roomNights);
        Task<IEnumerable<Booking>> GetRecentAsync(int take = 25);
        Task<IEnumerable<Booking>> GetRecentByBranchAsync(int branchId, int take = 25);
        Task<IEnumerable<Booking>> GetByBranchAndDateRangeAsync(int branchId, DateTime? fromDate, DateTime? toDate, int take = 100);
        Task<Booking?> GetByBookingNumberAsync(string bookingNumber);
        Task<int> GetTodayBookingCountAsync();
        Task<decimal> GetTodayAdvanceAmountAsync();
        Task<int> GetTodayCheckInCountAsync();
        Task<int> GetTodayCheckOutCountAsync();
        Task<bool> UpdateRoomAssignmentAsync(string bookingNumber, int roomId);
        Task<bool> UpdateBookingDatesAsync(string bookingNumber, DateTime checkInDate, DateTime checkOutDate, int nights, decimal baseAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount, decimal totalAmount);
        Task<bool> UpdateRoomTypeAsync(string bookingNumber, int newRoomTypeId, decimal baseAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount, decimal totalAmount);
        Task<bool> UpdateActualCheckOutDateAsync(string bookingNumber, DateTime actualCheckOutDate, int performedBy);
        Task<IEnumerable<BookingAuditLog>> GetAuditLogAsync(int bookingId);
        Task AddAuditLogAsync(int bookingId, string bookingNumber, string actionType, string description, string? oldValue = null, string? newValue = null, int? performedBy = null);
        Task<IEnumerable<BookingPayment>> GetPaymentsAsync(int bookingId);
        Task<bool> AddPaymentAsync(BookingPayment payment, int performedBy);
        Task<Booking?> GetLastBookingByGuestPhoneAsync(string phone);
        Task<bool> AddGuestToBookingAsync(BookingGuest guest, int branchId);
        Task<bool> UpdateGuestAsync(BookingGuest guest);
        Task<bool> DeleteGuestAsync(int guestId, int deletedBy);
    }
}
