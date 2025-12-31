namespace HotelApp.Web.Models
{
    public enum OtherChargeType
    {
        CarRental = 1,
        Fitness = 2,
        Laundry = 3,
        Others = 4,
        TourAndTravel = 5,
        FoodAndBeverage = 6
    }

    public class OtherCharge
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public OtherChargeType Type { get; set; } = OtherChargeType.Others;
        public decimal Rate { get; set; }

        // Percent fields
        public decimal GSTPercent { get; set; }
        public decimal CGSTPercent { get; set; }
        public decimal SGSTPercent { get; set; }

        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
