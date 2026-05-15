namespace HotelApp.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public string? ExceptionType { get; set; }
    public bool IsDatabaseError { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
