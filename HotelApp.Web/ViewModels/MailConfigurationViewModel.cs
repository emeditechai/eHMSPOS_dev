using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.ViewModels
{
    public class MailConfigurationViewModel
    {
        public int Id { get; set; }
        public int BranchID { get; set; }

        [Display(Name = "SMTP Server")]
        public string? SmtpHost { get; set; }

        [Display(Name = "SMTP Port")]
        public int? SmtpPort { get; set; }

        [Display(Name = "Username")]
        public string? UserName { get; set; }

        [Display(Name = "Password")]
        public string? Password { get; set; }

        public bool HasPassword { get; set; }

        [Display(Name = "Enable SSL/TLS")]
        public bool EnableSslTls { get; set; } = true;

        [Display(Name = "From Email")]
        public string? FromEmail { get; set; }

        [Display(Name = "From Name")]
        public string? FromName { get; set; }

        [Display(Name = "Admin Notification Email")]
        public string? AdminNotificationEmail { get; set; }

        [Display(Name = "Activate Email Service")]
        public bool IsActive { get; set; } = false;

        // Test send
        [Display(Name = "Test To Email")]
        public string? TestToEmail { get; set; }
    }
}
