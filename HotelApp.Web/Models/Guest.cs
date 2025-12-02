namespace HotelApp.Web.Models
{
    public class Guest
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? IdentityType { get; set; }
        public string? IdentityNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? LoyaltyId { get; set; }
        public string GuestType { get; set; } = "Primary"; // Primary, Companion, Child
        public int? ParentGuestId { get; set; }
        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        
        // Navigation property
        public Guest? ParentGuest { get; set; }
        public List<Guest> ChildGuests { get; set; } = new();
    }
}
