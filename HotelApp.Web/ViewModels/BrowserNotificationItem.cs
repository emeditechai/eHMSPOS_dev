namespace HotelApp.Web.ViewModels;

public sealed class BrowserNotificationItem
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Url { get; init; }
    public required string Kind { get; init; }
}
