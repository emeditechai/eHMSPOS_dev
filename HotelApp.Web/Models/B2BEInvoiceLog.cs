namespace HotelApp.Web.Models
{
    public class B2BEInvoiceLog
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string BookingNo { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Unique version number in "1.1" format (e.g. 1.1, 1.2, 1.3 ...)
        /// Generated from EInvoiceVersionSequence table.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Reflects the EInvoiceMode from Hotel Settings at the time of generation: "MANUAL" or "AUTO".
        /// </summary>
        public string GenerationType { get; set; } = string.Empty;

        /// <summary>
        /// The full e-invoice JSON payload as a string.
        /// </summary>
        public string JsonPayload { get; set; } = string.Empty;

        public int BranchID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        // ── Portal push / IRN tracking ─────────────────────────────────────────
        public string? PushStatus { get; set; }   // null | PENDING | PUSHED | FAILED
        public DateTime? PushedAt { get; set; }
        public string? PushResponse { get; set; }
        // ── IRP response fields (AUTO mode) ──────────────────────────────────
        /// <summary>Invoice Reference Number from the GST portal.</summary>
        public string? Irn { get; set; }
        /// <summary>Acknowledgement number from IRP.</summary>
        public string? AckNo { get; set; }
        /// <summary>Acknowledgement date/time string from IRP.</summary>
        public string? AckDt { get; set; }
        /// <summary>Base-64 encoded signed QR code from IRP.</summary>
        public string? SignedQRCode { get; set; }
        /// <summary>Raw JSON sent to IRP for IRN generation.</summary>
        public string? IrnRequestJson { get; set; }
        /// <summary>Raw JSON response received from IRP.</summary>
        public string? IrnResponseJson { get; set; }
    }
}
