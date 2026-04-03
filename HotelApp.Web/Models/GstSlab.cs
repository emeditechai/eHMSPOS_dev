using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    public class GstSlab
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Slab Code")]
        public string SlabCode { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        [Display(Name = "Slab Name")]
        public string SlabName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Effective From")]
        public DateTime EffectiveFrom { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Effective To")]
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int ActiveBandCount { get; set; }
        public decimal? MinimumTariffFrom { get; set; }
        public decimal? MaximumTariffTo { get; set; }
        public decimal? MaximumGstPercent { get; set; }
        public IList<GstSlabBand> TariffBands { get; set; } = new List<GstSlabBand>();
    }

    public class GstSlabBand
    {
        public int Id { get; set; }
        public int GstSlabId { get; set; }
        public string? SlabCode { get; set; }
        public string? SlabName { get; set; }

        [Range(0, 999999999.99)]
        [Display(Name = "Tariff From")]
        public decimal TariffFrom { get; set; }

        [Range(0, 999999999.99)]
        [Display(Name = "Tariff To")]
        public decimal? TariffTo { get; set; }

        [Range(0, 100)]
        [Display(Name = "GST %")]
        public decimal GstPercent { get; set; }

        [Range(0, 100)]
        [Display(Name = "CGST %")]
        public decimal CgstPercent { get; set; }

        [Range(0, 100)]
        [Display(Name = "SGST %")]
        public decimal SgstPercent { get; set; }

        [Range(0, 100)]
        [Display(Name = "IGST %")]
        public decimal IgstPercent { get; set; }

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}