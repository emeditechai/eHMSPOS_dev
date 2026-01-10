namespace HotelApp.Web.Models
{
    public class BookingReceiptTemplateSettings
    {
        public int Id { get; set; }
        public int BranchID { get; set; }
        public string TemplateKey { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }
    }
}
