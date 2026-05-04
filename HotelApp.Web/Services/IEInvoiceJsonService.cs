using HotelApp.Web.Models;

namespace HotelApp.Web.Services
{
    public interface IEInvoiceJsonService
    {
        /// <summary>
        /// Generates and persists an e-invoice JSON log for the given B2B booking
        /// if EInvoiceMode is MANUAL and the booking is B2B.
        /// Returns the generated log, or null when generation is skipped.
        /// </summary>
        Task<B2BEInvoiceLog?> GenerateAndSaveAsync(Booking booking, HotelSettings hotelSettings, int? createdBy);
    }
}
