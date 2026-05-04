namespace HotelApp.Web.Models
{
    /// <summary>
    /// Represents a cached IRP (Invoice Registration Portal) auth token for a branch.
    /// Used to avoid re-authenticating on every invoice submission.
    /// </summary>
    public class EInvoiceIrpToken
    {
        public int Id { get; set; }
        public int BranchID { get; set; }
        public int? SessionUserId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}
