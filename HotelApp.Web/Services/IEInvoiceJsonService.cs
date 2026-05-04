using HotelApp.Web.Models;

namespace HotelApp.Web.Services
{
    public interface IEInvoiceJsonService
    {
        /// <summary>
        /// Generates and persists an e-invoice JSON log for the given B2B booking.
        /// - MANUAL mode: saves log + optionally exports JSON file to EInvoiceJsonStoragePath.
        /// - AUTO mode: saves log + authenticates with IRP + submits for IRN + updates log with response.
        /// Returns the generated log, or null when generation is skipped.
        /// </summary>
        Task<B2BEInvoiceLog?> GenerateAndSaveAsync(Booking booking, HotelSettings hotelSettings, int? createdBy);
    }
}
