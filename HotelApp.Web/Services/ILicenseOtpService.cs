using HotelApp.Web.Models;

namespace HotelApp.Web.Services;

public interface ILicenseOtpService
{
    /// <summary>
    /// Generates a 6-digit OTP, stores it in the remote ClientOTPValidationHistory
    /// table, and sends it to all active approver emails.
    /// </summary>
    Task<(bool Success, string Message)> SendOtpAsync(string clientCode, string registrantEmail);

    /// <summary>
    /// Validates the submitted OTP against the most recent unvalidated entry
    /// in remote ClientOTPValidationHistory for the given clientCode.
    /// OTP is valid for 60 seconds only.
    /// </summary>
    Task<(bool Success, string Message)> ValidateOtpAsync(string clientCode, string otp);

    /// <summary>
    /// Sends a Welcome email to the newly-registered client's own email address
    /// with all license details: ClientCode, LicenseKey, hardware IDs, dates, AppUrl.
    /// Best-effort — failure is logged but does not block registration.
    /// </summary>
    Task SendWelcomeEmailAsync(ClientAppLicense license);

    /// <summary>
    /// Sends a hardware re-registration confirmation email to the client's
    /// registered email address after a successful hardware update and re-validation.
    /// Best-effort — failure is logged but does not block the renewal flow.
    /// </summary>
    Task SendHardwareRenewalNotificationAsync(
        string clientCode, string clientName, string emailId, string appUrl,
        string macId, string hddSerial, string mbSerial);
}
