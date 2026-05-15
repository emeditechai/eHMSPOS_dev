using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    // ── Enums ────────────────────────────────────────────────────────────────

    public enum BanquetVenueType
    {
        Banquet = 1,
        Conference = 2,
        Lawn = 3,
        Rooftop = 4,
        BoardRoom = 5,
        Exhibition = 6,
        Other = 7
    }

    public enum BanquetAddonServiceType
    {
        AVEquipment = 1,
        Decoration = 2,
        Photography = 3,
        Videography = 4,
        Entertainment = 5,
        Parking = 6,
        Transportation = 7,
        Floral = 8,
        Stationery = 9,
        Gifts = 10,
        SecurityService = 11,
        Other = 12
    }

    public enum BanquetPackageType
    {
        VegMenu = 1,
        NonVegMenu = 2,
        MixedMenu = 3,
        SpecialMenu = 4,
        JainMenu = 5
    }

    public enum BanquetStatus
    {
        Inquiry = 1,
        Tentative = 2,
        Confirmed = 3,
        CheckedIn = 4,
        EventComplete = 5,
        Cancelled = 6,
        NoShow = 7
    }

    public enum BanquetPaymentStatus
    {
        Pending = 1,
        PartialPaid = 2,
        FullPaid = 3
    }

    public enum BanquetMealType
    {
        Veg = 1,
        NonVeg = 2,
        Mixed = 3,
        Jain = 4
    }

    public enum BanquetRateType
    {
        PerEvent = 1,
        PerHour = 2,
        PerPax = 3,
        PerItem = 4
    }

    public enum BanquetVenueHireType
    {
        FullDay = 1,
        HalfDay = 2,
        Custom = 3
    }

    // ── Master: BanquetVenue ──────────────────────────────────────────────────

    public class BanquetVenue
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Venue Code")]
        public string VenueCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Venue Name")]
        public string VenueName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Venue Type")]
        public string VenueType { get; set; } = "Banquet";

        [Display(Name = "Seated Capacity")]
        public int CapacitySeated { get; set; }

        [Display(Name = "Buffet Capacity")]
        public int CapacityBuffet { get; set; }

        [Display(Name = "Theater Capacity")]
        public int CapacityTheater { get; set; }

        [Display(Name = "Cocktail Capacity")]
        public int CapacityCockTail { get; set; }

        [Display(Name = "Area (Sq. Ft)")]
        public decimal? Area_SqFt { get; set; }

        [Display(Name = "Floor")]
        public int? FloorId { get; set; }
        public string? FloorName { get; set; }

        [Required]
        [Display(Name = "Rate Per Day")]
        public decimal BaseRatePerDay { get; set; }

        [Display(Name = "Rate Per Half Day")]
        public decimal BaseRatePerHalfDay { get; set; }

        // All GST fields user-configurable
        [Display(Name = "GST %")]
        public decimal GSTPercent { get; set; }

        [Display(Name = "CGST %")]
        public decimal CGSTPercent { get; set; }

        [Display(Name = "SGST %")]
        public decimal SGSTPercent { get; set; }

        [Display(Name = "IGST %")]
        public decimal IGSTPercent { get; set; }

        [Display(Name = "SAC Code")]
        [StringLength(10)]
        public string? SACCode { get; set; }

        [Display(Name = "GST Slab")]
        public int? GstSlabId { get; set; }

        // Populated via JOIN — not a direct DB column
        public string? GstSlabName { get; set; }

        [Display(Name = "Air Conditioned")]
        public bool IsAC { get; set; }

        [Display(Name = "Stage")]
        public bool HasStage { get; set; }

        [Display(Name = "Projector / LED")]
        public bool HasProjector { get; set; }

        [Display(Name = "Sound System")]
        public bool HasSoundSystem { get; set; }

        [Display(Name = "Parking")]
        public bool HasParking { get; set; }

        [Display(Name = "In-House Catering")]
        public bool HasCatering { get; set; }

        [Display(Name = "Wi-Fi")]
        public bool HasWifi { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public string? PhotoPath { get; set; }
        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

    // ── Master: BanquetEventType ──────────────────────────────────────────────

    public class BanquetEventType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Event Type Code")]
        public string EventTypeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Event Type Name")]
        public string EventTypeName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        [Display(Name = "Icon Class")]
        public string? IconClass { get; set; } = "fas fa-calendar-star";

        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

    // ── Master: BanquetPackage ────────────────────────────────────────────────

    public class BanquetPackage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Package Code")]
        public string PackageCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Package Name")]
        public string PackageName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Package Type")]
        public string PackageType { get; set; } = "VegMenu";

        [Required]
        [Display(Name = "Price Per Pax (₹)")]
        public decimal PricePerPax { get; set; }

        [Display(Name = "Minimum Guarantee Pax")]
        public int MinimumGuaranteePax { get; set; }

        // All GST user-configurable
        [Display(Name = "GST %")]
        public decimal GSTPercent { get; set; }

        [Display(Name = "CGST %")]
        public decimal CGSTPercent { get; set; }

        [Display(Name = "SGST %")]
        public decimal SGSTPercent { get; set; }

        [Display(Name = "IGST %")]
        public decimal IGSTPercent { get; set; }

        [Display(Name = "SAC Code")]
        [StringLength(10)]
        public string? SACCode { get; set; }

        [Display(Name = "GST Slab")]
        public int? GstSlabId { get; set; }

        // Populated via JOIN — not a direct DB column
        public string? GstSlabName { get; set; }

        [Display(Name = "Starters")]
        public bool IncludesStarter { get; set; }

        [Display(Name = "Main Course")]
        public bool IncludesMainCourse { get; set; } = true;

        [Display(Name = "Dessert")]
        public bool IncludesDessert { get; set; }

        [Display(Name = "Beverages")]
        public bool IncludesBeverages { get; set; }

        [Display(Name = "Live Counter")]
        public bool IncludesLive { get; set; }

        [Display(Name = "Menu Description")]
        public string? MenuDescription { get; set; }

        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

    // ── Master: BanquetAddonService ───────────────────────────────────────────

    public class BanquetAddonService
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Service Code")]
        public string ServiceCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Service Name")]
        public string ServiceName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Service Type")]
        public string ServiceType { get; set; } = "Other";

        [Required]
        [Display(Name = "Rate (₹)")]
        public decimal Rate { get; set; }

        [Required]
        [Display(Name = "Rate Type")]
        public string RateType { get; set; } = "PerEvent";

        // All GST user-configurable
        [Display(Name = "GST %")]
        public decimal GSTPercent { get; set; }

        [Display(Name = "CGST %")]
        public decimal CGSTPercent { get; set; }

        [Display(Name = "SGST %")]
        public decimal SGSTPercent { get; set; }

        [Display(Name = "IGST %")]
        public decimal IGSTPercent { get; set; }

        [Display(Name = "SAC Code")]
        [StringLength(10)]
        public string? SACCode { get; set; }

        [Display(Name = "GST Slab")]
        public int? GstSlabId { get; set; }

        // Populated via JOIN — not a direct DB column
        public string? GstSlabName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
