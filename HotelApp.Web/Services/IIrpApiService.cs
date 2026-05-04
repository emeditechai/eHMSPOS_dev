namespace HotelApp.Web.Services
{
    /// <summary>
    /// Response from IRP's IRN generation endpoint.
    /// </summary>
    public class IrpIrnResponse
    {
        public bool Success { get; set; }
        public string? Irn { get; set; }
        public string? AckNo { get; set; }
        public string? AckDt { get; set; }
        public string? SignedQRCode { get; set; }
        public string? RawRequest { get; set; }
        public string? RawResponse { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IIrpApiService
    {
        /// <summary>
        /// Returns a valid access token for the given branch/settings.
        /// Checks DB cache first; re-authenticates when expired or missing.
        /// </summary>
        Task<string?> GetValidTokenAsync(HotelApp.Web.Models.HotelSettings settings, int branchId, int? userId);

        /// <summary>
        /// Submits an invoice JSON to the IRP Generate IRN endpoint.
        /// </summary>
        Task<IrpIrnResponse> GenerateIrnAsync(HotelApp.Web.Models.HotelSettings settings, string accessToken, string invoiceJson);
    }
}
