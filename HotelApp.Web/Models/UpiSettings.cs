using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    public class UpiSettings
    {
        public int Id { get; set; }
        public int BranchID { get; set; }

        [Display(Name = "UPI ID (VPA)")]
        [StringLength(100)]
        public string? UpiVpa { get; set; }

        [Display(Name = "Business/Hotel Name")]
        [StringLength(100)]
        public string? PayeeName { get; set; }

        [Display(Name = "Enable UPI Payments")]
        public bool IsEnabled { get; set; } = false;

        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }
    }
}
