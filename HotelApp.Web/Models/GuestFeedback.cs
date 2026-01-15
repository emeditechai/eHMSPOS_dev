namespace HotelApp.Web.Models
{
    public class GuestFeedback
    {
        public int Id { get; set; }
        public int BranchID { get; set; }

        public int? BookingId { get; set; }
        public string? BookingNumber { get; set; }
        public string? RoomNumber { get; set; }
        public DateTime VisitDate { get; set; }

        public string? GuestName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? Birthday { get; set; }
        public DateTime? Anniversary { get; set; }
        public bool? IsFirstVisit { get; set; }

        public byte OverallRating { get; set; }
        public byte? RoomCleanlinessRating { get; set; }
        public byte? StaffBehaviorRating { get; set; }
        public byte? ServiceRating { get; set; }
        public byte? RoomComfortRating { get; set; }
        public byte? AmenitiesRating { get; set; }
        public byte? FoodRating { get; set; }
        public byte? ValueForMoneyRating { get; set; }
        public byte? CheckInExperienceRating { get; set; }

        public string? QuickTags { get; set; }
        public string? Comments { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
