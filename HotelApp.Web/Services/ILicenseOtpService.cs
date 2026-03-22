namespace HotelApp.Web.Services;

public interface ILicenseOtpService
{
    /// <summary>
    /// Generates a 6-digit OTP, stores it in the remote ClientOTPValidationHistory
    /// table, and sends it to all active approver emails.
    /// Returns the generated ClientCode assigned to this registration attempt.
    /// </summary>
    Task<(bool Success, string Message)> SendOtpAsync(string clientCode, string registrantEmail);

    /// <summary>
    /// Validates the submitted OTP against the most recent unvalidated entry
    /// in remote ClientOTPValidationHistory for the given clientCode.
    /// OTP is valid for 60 seconds only.
    /// </summary>
    Task<(bool Success, string Message)> ValidateOtpAsync(string clientCode, string otp);
}
