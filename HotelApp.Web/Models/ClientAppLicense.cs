namespace HotelApp.Web.Models;

public class ClientAppLicense
{
    public long Id { get; set; }
    public string? ClientCode { get; set; }
    public string? ClientName { get; set; }
    public string? ContactNumber { get; set; }
    public string? LicenseKey { get; set; }
    public string? HardDiskNumber { get; set; }
    public string? ServerMacID { get; set; }
    public string? MotherboardNumber { get; set; }
    public DateTime? Startdate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public bool OTP_Verified { get; set; }
    public string? PublicIPAddress { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string? EmailID { get; set; }
    public DateTime? AMC_Expireddate { get; set; }
    public string? AppUrl { get; set; }
    public string? ProductType { get; set; }
    public string? ConnectionString { get; set; }

    // Alert columns
    public bool IsDisplayAlerts { get; set; }
    public DateTime? AlertStartDate { get; set; }
    public TimeSpan? AlertStartTime { get; set; }
    public DateTime? AlertEndDate { get; set; }
    public TimeSpan? AlertEndTime { get; set; }
    public string? AlertMessage { get; set; }
}
