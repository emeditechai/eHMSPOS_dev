namespace HotelApp.Web.Models
{
    public class BookingOtherCharge
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int OtherChargeId { get; set; }
        public decimal Rate { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
