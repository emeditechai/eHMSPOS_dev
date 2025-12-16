using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IBookingOtherChargeRepository
    {
        Task<IEnumerable<BookingOtherChargeDetailRow>> GetDetailsByBookingIdAsync(int bookingId);
        Task UpsertForBookingAsync(int bookingId, IReadOnlyList<BookingOtherChargeUpsertRow> rows, int? performedBy);
    }

    public class BookingOtherChargeDetailRow
    {
        public int OtherChargeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal CGSTPercent { get; set; }
        public decimal SGSTPercent { get; set; }
        public int Qty { get; set; }
        public decimal Rate { get; set; }
        public string? Note { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
    }

    public class BookingOtherChargeUpsertRow
    {
        public int OtherChargeId { get; set; }
        public int Qty { get; set; }
        public decimal Rate { get; set; }
        public string? Note { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
    }
}
