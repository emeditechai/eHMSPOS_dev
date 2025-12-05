namespace HotelApp.Web.Models
{
    public class RoomType
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BaseRate { get; set; }
        public int MaxOccupancy { get; set; }
        public int? Max_RoomAvailability { get; set; }
        public string? Amenities { get; set; }
        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? CreatedBy { get; set; }
        public int? LastModifiedBy { get; set; }
    }
}
