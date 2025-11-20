using System;

namespace HotelApp.Web.Models
{
    public class Floor
    {
        public int Id { get; set; }
        public string FloorName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? CreatedBy { get; set; }
        public int? LastModifiedBy { get; set; }
    }
}
