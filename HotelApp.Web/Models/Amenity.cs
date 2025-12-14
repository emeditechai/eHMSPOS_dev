namespace HotelApp.Web.Models
{
    public class Amenity
    {
        public int Id { get; set; }
        public string AmenityName { get; set; } = string.Empty;
        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
