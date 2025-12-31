namespace HotelApp.Web.Models
{
    public class PaymentQrOptions
    {
        public bool Enabled { get; set; } = false;

        // UPI VPA (Virtual Payment Address), e.g. "hotelname@bank"
        public string? UpiVpa { get; set; }

        // Display name shown in UPI apps (optional)
        public string? PayeeName { get; set; }

        // Currency code used in UPI URIs (default INR)
        public string Currency { get; set; } = "INR";

        // Optional note template; supports {bookingNumber}
        public string NoteTemplate { get; set; } = "Booking {bookingNumber}";
    }
}
