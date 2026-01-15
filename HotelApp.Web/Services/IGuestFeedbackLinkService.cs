using System;

namespace HotelApp.Web.Services
{
    public record GuestFeedbackLinkPayload(string BookingNumber, int BranchId, DateTimeOffset ExpiresUtc);

    public interface IGuestFeedbackLinkService
    {
        string CreateToken(string bookingNumber, int branchId, DateTimeOffset expiresUtc);
        bool TryValidateToken(string token, out GuestFeedbackLinkPayload payload);
    }
}
