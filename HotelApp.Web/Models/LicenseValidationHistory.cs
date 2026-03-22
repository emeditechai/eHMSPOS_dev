namespace HotelApp.Web.Models;

public class LicenseValidationHistory
{
    public long Id { get; set; }
    public string? ClientCode { get; set; }
    public string? LicenseKey { get; set; }
    public bool IsValid { get; set; }
    public string? FailureReason { get; set; }
    public string? PublicIPAddress { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AppUrl { get; set; }
    public string? ProductType { get; set; }
}
