using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    public class MailConfiguration
    {
        public int Id { get; set; }
        public int BranchID { get; set; }

        [Display(Name = "SMTP Server")]
        [StringLength(200)]
        public string? SmtpHost { get; set; }

        [Display(Name = "SMTP Port")]
        public int? SmtpPort { get; set; }

        [Display(Name = "Username")]
        [StringLength(200)]
        public string? UserName { get; set; }

        // Stored encrypted/protected at rest.
        public string? PasswordProtected { get; set; }

        [Display(Name = "Enable SSL/TLS")]
        public bool EnableSslTls { get; set; } = true;

        [Display(Name = "From Email")]
        [StringLength(200)]
        public string? FromEmail { get; set; }

        [Display(Name = "From Name")]
        [StringLength(200)]
        public string? FromName { get; set; }

        [Display(Name = "Admin Notification Email")]
        [StringLength(200)]
        public string? AdminNotificationEmail { get; set; }

        [Display(Name = "Activate Email Service")]
        public bool IsActive { get; set; } = false;

        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }
    }
}
