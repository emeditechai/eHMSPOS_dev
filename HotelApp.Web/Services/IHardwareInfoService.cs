namespace HotelApp.Web.Services;

public class HardwareInfo
{
    public string MacId { get; set; } = string.Empty;
    public string HardDiskSerial { get; set; } = string.Empty;
    public string MotherboardSerial { get; set; } = string.Empty;
}

public interface IHardwareInfoService
{
    HardwareInfo GetHardwareInfo();
}
