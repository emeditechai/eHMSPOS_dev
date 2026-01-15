using System;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace HotelApp.Web.Services
{
    public class GuestFeedbackLinkService : IGuestFeedbackLinkService
    {
        private readonly IDataProtector _protector;

        public GuestFeedbackLinkService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("HotelApp.GuestFeedback.LinkToken.v1");
        }

        public string CreateToken(string bookingNumber, int branchId, DateTimeOffset expiresUtc)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                throw new ArgumentException("Booking number is required.", nameof(bookingNumber));
            }

            if (branchId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(branchId), "BranchId must be positive.");
            }

            var payload = string.Join("|",
                bookingNumber.Trim(),
                branchId.ToString(CultureInfo.InvariantCulture),
                expiresUtc.UtcTicks.ToString(CultureInfo.InvariantCulture));

            var protectedPayload = _protector.Protect(payload);
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedPayload));
        }

        public bool TryValidateToken(string token, out GuestFeedbackLinkPayload payload)
        {
            payload = default!;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            try
            {
                var protectedPayload = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                var unprotected = _protector.Unprotect(protectedPayload);

                var parts = unprotected.Split('|');
                if (parts.Length != 3)
                {
                    return false;
                }

                var bookingNumber = parts[0].Trim();
                if (string.IsNullOrWhiteSpace(bookingNumber))
                {
                    return false;
                }

                if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var branchId) || branchId <= 0)
                {
                    return false;
                }

                if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var utcTicks) || utcTicks <= 0)
                {
                    return false;
                }

                var expiresUtc = new DateTimeOffset(utcTicks, TimeSpan.Zero);
                if (DateTimeOffset.UtcNow > expiresUtc)
                {
                    return false;
                }

                payload = new GuestFeedbackLinkPayload(bookingNumber, branchId, expiresUtc);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
