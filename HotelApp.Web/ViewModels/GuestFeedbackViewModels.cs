using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.ViewModels
{
    public class GuestFeedbackCreateViewModel
    {
        // Public link token (for anonymous access)
        public string? AccessToken { get; set; }

        public int? BookingId { get; set; }
        public string? BookingNumber { get; set; }
        public string? RoomNumber { get; set; }

        [Display(Name = "Visit Date")]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; } = DateTime.Today;

        [Display(Name = "Name")]
        [StringLength(120)]
        public string? GuestName { get; set; }

        [EmailAddress]
        [StringLength(120)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Anniversary { get; set; }

        [Display(Name = "First Visit")]
        public bool? IsFirstVisit { get; set; }

        [Range(1, 5)]
        [Display(Name = "Overall Rating")]
        public byte OverallRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Room Cleanliness")]
        public byte? RoomCleanlinessRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Staff Behavior")]
        public byte? StaffBehaviorRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Service")]
        public byte? ServiceRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Room Comfort")]
        public byte? RoomComfortRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Amenities")]
        public byte? AmenitiesRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Food")]
        public byte? FoodRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Value For Money")]
        public byte? ValueForMoneyRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Check-in Experience")]
        public byte? CheckInExperienceRating { get; set; }

        [StringLength(500)]
        public string? QuickTags { get; set; }

        [StringLength(1000)]
        public string? Comments { get; set; }

        // Display helpers (not posted)
        public string? HotelName { get; set; }
        public string? HotelAddress { get; set; }
        public string? HotelEmail { get; set; }
        public string? HotelWebsite { get; set; }
    }

    public class GuestFeedbackListItemViewModel
    {
        public int Id { get; set; }
        public DateTime VisitDate { get; set; }
        public string? BookingNumber { get; set; }
        public string? RoomNumber { get; set; }
        public string? GuestName { get; set; }
        public string? Phone { get; set; }
        public byte OverallRating { get; set; }
        public string? QuickTags { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class GuestFeedbackDetailsViewModel
    {
        public int Id { get; set; }
        public DateTime VisitDate { get; set; }
        public string? BookingNumber { get; set; }
        public string? RoomNumber { get; set; }
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

        public DateTime CreatedDate { get; set; }

        public string? HotelName { get; set; }
        public string? HotelAddress { get; set; }
        public string? HotelEmail { get; set; }
        public string? HotelWebsite { get; set; }
    }
}
