using Dapper;
using HotelApp.Web.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;

namespace HotelApp.Web.Services;

/// <summary>
/// Sends and validates registration OTPs using:
/// - SMTP config from remote tbl_centralmailconfiguration
/// - Approver email list from remote ApproverMailIDs
/// - OTP history stored in remote ClientOTPValidationHistory
/// </summary>
public class LicenseOtpService : ILicenseOtpService
{
    // Remote Central License DB — credentials stored in code per security requirement
    private const string RemoteConnStr =
        "Server=198.38.81.123;Database=Central_Lic_DB;User Id=sa;Password=asdf@1234;TrustServerCertificate=True;";

    private readonly ILogger<LicenseOtpService> _logger;

    public LicenseOtpService(ILogger<LicenseOtpService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendOtpAsync(string clientCode, string registrantEmail)
    {
        try
        {
            // 1. Get SMTP config from remote DB
            var smtp = await GetSmtpConfigAsync();
            if (smtp == null)
                return (false, "SMTP configuration not found in central database.");

            // 2. Get approver email list from remote DB
            var approverEmails = (await GetApproverEmailsAsync()).ToList();
            if (approverEmails.Count == 0)
                return (false, "No approver emails configured. Please contact the system administrator.");

            // 3. Generate OTP
            var otp = GenerateOtp();

            // 4. Persist OTP to remote ClientOTPValidationHistory
            await SaveOtpAsync(clientCode, otp);

            // 5. Send OTP email to active approvers ONLY.
            // The registrant's email is intentionally excluded — OTP must be
            // authorised by an approver, not acknowledged by the client.
            var recipients = approverEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var subject = $"[eLUX Stay] License Registration OTP — {clientCode}";
            var htmlBody = BuildOtpEmailHtml(clientCode, otp);

            await SendEmailAsync(smtp, recipients, subject, htmlBody);

            _logger.LogInformation("OTP sent for client {ClientCode} to {Count} recipients.", clientCode, recipients.Count);
            return (true, "OTP sent successfully. Please check your email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP for client {ClientCode}.", clientCode);
            return (false, $"Failed to send OTP: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ValidateOtpAsync(string clientCode, string otp)
    {
        try
        {
            await using var conn = new SqlConnection(RemoteConnStr);

            const string sql = @"
                SELECT TOP 1 ID, OTP, isValidated, Createdate
                FROM ClientOTPValidationHistory
                WHERE ClientCode = @ClientCode
                  AND OTPType    = 'Registration'
                  AND isValidated = 0
                ORDER BY Createdate DESC";

            var record = await conn.QueryFirstOrDefaultAsync<OtpRecord>(sql, new { ClientCode = clientCode });

            if (record == null)
                return (false, "No pending OTP found. Please request a new OTP.");

            // 60-second validity window
            if ((DateTime.Now - record.Createdate).TotalSeconds > 60)
                return (false, "OTP has expired. Please request a new OTP.");

            if (!string.Equals(record.OTP.Trim(), otp.Trim(), StringComparison.Ordinal))
                return (false, "Invalid OTP. Please try again.");

            // Mark as validated
            await conn.ExecuteAsync(@"
                UPDATE ClientOTPValidationHistory
                SET    isValidated = 1,
                       OTPValidatedatetime = GETDATE()
                WHERE  ID = @Id",
                new { record.Id });

            return (true, "OTP validated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP validation failed for client {ClientCode}.", clientCode);
            return (false, $"OTP validation error: {ex.Message}");
        }
    }

    // ─── Public notification methods ─────────────────────────────────────────

    public async Task SendWelcomeEmailAsync(ClientAppLicense license)
    {
        if (string.IsNullOrWhiteSpace(license.EmailID))
        {
            _logger.LogWarning("SendWelcomeEmail: no client email on record for {ClientCode} — skipping.", license.ClientCode);
            return;
        }

        try
        {
            var smtp = await GetSmtpConfigAsync();
            if (smtp == null)
            {
                _logger.LogWarning("SendWelcomeEmail: SMTP config not found — skipping for {ClientCode}.", license.ClientCode);
                return;
            }

            var subject  = $"[eLUX Stay] Welcome — License Registered Successfully ({license.ClientCode})";
            var htmlBody = BuildWelcomeEmailHtml(license);

            await SendEmailAsync(smtp, new List<string> { license.EmailID }, subject, htmlBody);

            _logger.LogInformation("Welcome email sent for {ClientCode} to {Email}.", license.ClientCode, license.EmailID);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Welcome email failed for {ClientCode} — non-critical.", license.ClientCode);
        }
    }

    public async Task SendHardwareRenewalNotificationAsync(
        string clientCode, string clientName, string emailId, string appUrl,
        string macId, string hddSerial, string mbSerial)
    {
        if (string.IsNullOrWhiteSpace(emailId))
        {
            _logger.LogWarning("HardwareRenewalNotification: no client email for {ClientCode} — skipping.", clientCode);
            return;
        }

        try
        {
            var smtp = await GetSmtpConfigAsync();
            if (smtp == null)
            {
                _logger.LogWarning("HardwareRenewalNotification: SMTP config not found — skipping for {ClientCode}.", clientCode);
                return;
            }

            var subject  = $"[eLUX Stay] Hardware Re-registration Confirmed ({clientCode})";
            var htmlBody = BuildHardwareRenewalEmailHtml(clientCode, clientName, appUrl, macId, hddSerial, mbSerial);

            await SendEmailAsync(smtp, new List<string> { emailId }, subject, htmlBody);

            _logger.LogInformation("Hardware renewal notification sent for {ClientCode} to {Email}.", clientCode, emailId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hardware renewal notification failed for {ClientCode} — non-critical.", clientCode);
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static string GenerateOtp()
    {
        var rng = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100_000, 1_000_000);
        return rng.ToString("D6");
    }

    private async Task SaveOtpAsync(string clientCode, string otp)
    {
        await using var conn = new SqlConnection(RemoteConnStr);
        await conn.ExecuteAsync(@"
            INSERT INTO ClientOTPValidationHistory
                   (ClientCode, OTPType, OTP, isValidated, Createdate)
            VALUES (@ClientCode, 'Registration', @OTP, 0, GETDATE())",
            new { ClientCode = clientCode, OTP = otp });
    }

    private async Task<SmtpConfig?> GetSmtpConfigAsync()
    {
        await using var conn = new SqlConnection(RemoteConnStr);
        return await conn.QueryFirstOrDefaultAsync<SmtpConfig>(@"
            SELECT TOP 1
                   SmtpServer, SmtpPort, SmtpUsername, SmtpPassword,
                   EnableSSL, FromEmail, FromName
            FROM   tbl_centralmailconfiguration
            WHERE  IsActive = 1
            ORDER BY Id DESC");
    }

    private async Task<IEnumerable<string>> GetApproverEmailsAsync()
    {
        await using var conn = new SqlConnection(RemoteConnStr);
        return await conn.QueryAsync<string>(@"
            SELECT mailID FROM ApproverMailIDs WHERE IsActive = 1");
    }

    private static async Task SendEmailAsync(
        SmtpConfig config,
        IList<string> recipients,
        string subject,
        string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName ?? config.FromEmail, config.FromEmail));

        foreach (var r in recipients)
            message.To.Add(MailboxAddress.Parse(r));

        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        var secureOption = config.EnableSSL
            ? (config.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.None;

        await client.ConnectAsync(config.SmtpServer, config.SmtpPort, secureOption);
        await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static string BuildOtpEmailHtml(string clientCode, string otp)
    {
        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family:Arial,sans-serif;background:#f4f4f4;padding:20px;'>
  <div style='max-width:520px;margin:auto;background:#fff;border-radius:8px;
              box-shadow:0 2px 8px rgba(0,0,0,.1);padding:32px;'>
    <h2 style='color:#6c3fe8;margin-top:0;'>eLUX Stay — License Registration</h2>
    <p style='color:#333;'>A new license registration request has been received.</p>
    <table style='width:100%;margin-bottom:16px;'>
      <tr>
        <td style='color:#666;padding:4px 0;'>Client Code</td>
        <td style='color:#333;font-weight:bold;padding:4px 0;'>{clientCode}</td>
      </tr>
    </table>
    <p style='color:#333;'>Your One-Time Password (OTP) for registration:</p>
    <div style='text-align:center;margin:24px 0;'>
      <span style='font-size:40px;font-weight:bold;letter-spacing:10px;
                   color:#6c3fe8;background:#f0ebff;padding:12px 24px;
                   border-radius:8px;display:inline-block;'>{otp}</span>
    </div>
    <p style='color:#e53935;font-size:13px;'>
      &#9888; This OTP is valid for <strong>60 seconds</strong> only.
      Do not share it with anyone.
    </p>
    <hr style='border:none;border-top:1px solid #eee;margin:24px 0;'/>
    <p style='color:#999;font-size:12px;'>
      This email was sent by eLUX Stay License Management System.<br/>
      If you did not initiate this request, please ignore this email.
    </p>
  </div>
</body>
</html>";
    }

    private static string BuildWelcomeEmailHtml(ClientAppLicense lic)
    {
        string Row(string label, string? value) =>
            $"<tr><td style='color:#6b7280;padding:7px 0;font-size:.875rem;width:42%;'>{label}</td>" +
            $"<td style='color:#111827;font-weight:600;padding:7px 0;font-size:.875rem;'>{value ?? "—"}</td></tr>";

        var expiryStr   = lic.ExpiryDate.HasValue    ? lic.ExpiryDate.Value.ToString("dd-MMM-yyyy")      : "—";
        var amcStr      = lic.AMC_Expireddate.HasValue ? lic.AMC_Expireddate.Value.ToString("dd-MMM-yyyy") : "—";
        var startStr    = lic.Startdate.HasValue      ? lic.Startdate.Value.ToString("dd-MMM-yyyy")       : "—";

        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/><meta name='viewport' content='width=device-width'/></head>
<body style='font-family:""Segoe UI"",Arial,sans-serif;background:#f3f4f6;padding:24px;margin:0;'>
  <div style='max-width:580px;margin:auto;background:#fff;border-radius:12px;
              box-shadow:0 4px 16px rgba(0,0,0,.08);overflow:hidden;'>

    <!-- Header -->
    <div style='background:linear-gradient(135deg,#4f46e5,#7c3aed);padding:32px 36px;'>
      <p style='margin:0 0 4px;color:#c7d2fe;font-size:.78rem;letter-spacing:.8px;
                text-transform:uppercase;'>eLUX Stay &mdash; License Management</p>
      <h1 style='margin:0;color:#fff;font-size:1.45rem;font-weight:700;'>Welcome Aboard!</h1>
      <p style='margin:8px 0 0;color:#c7d2fe;font-size:.9rem;'>Your license has been registered successfully.</p>
    </div>

    <!-- Body -->
    <div style='padding:28px 36px;'>
      <p style='color:#374151;font-size:.9rem;margin-top:0;'>
        Dear <strong>{lic.ClientName}</strong>,<br/>
        Your <strong>eLUX Stay</strong> software license has been activated.
        Please keep the details below in a safe place &mdash; you will need the
        License Key if you ever need to re-register hardware.
      </p>

      <!-- Client Details -->
      <div style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;
                  padding:18px 20px;margin:18px 0;'>
        <p style='margin:0 0 10px;font-size:.7rem;font-weight:700;letter-spacing:.7px;
                  text-transform:uppercase;color:#6d28d9;'>Client Information</p>
        <table style='width:100%;border-collapse:collapse;'>
          {Row("Client Code",      lic.ClientCode)}
          {Row("Client Name",      lic.ClientName)}
          {Row("Contact Number",   lic.ContactNumber)}
          {Row("Email Address",    lic.EmailID)}
        </table>
      </div>

      <!-- License Details -->
      <div style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;
                  padding:18px 20px;margin:18px 0;'>
        <p style='margin:0 0 10px;font-size:.7rem;font-weight:700;letter-spacing:.7px;
                  text-transform:uppercase;color:#6d28d9;'>License Details</p>
        <table style='width:100%;border-collapse:collapse;'>
          {Row("License Key",      lic.LicenseKey)}
          {Row("Application URL",  lic.AppUrl)}
          {Row("Start Date",       startStr)}
          {Row("Expiry Date",      expiryStr)}
          {Row("AMC Expiry",       amcStr)}
          {Row("Product",          lic.ProductType ?? "eLuxstay")}
        </table>
      </div>

      <!-- Hardware Details -->
      <div style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;
                  padding:18px 20px;margin:18px 0;'>
        <p style='margin:0 0 10px;font-size:.7rem;font-weight:700;letter-spacing:.7px;
                  text-transform:uppercase;color:#6d28d9;'>Registered Hardware</p>
        <table style='width:100%;border-collapse:collapse;'>
          {Row("MAC Address",      lic.ServerMacID)}
          {Row("Hard Disk Serial", lic.HardDiskNumber)}
          {Row("Motherboard ID",   lic.MotherboardNumber)}
        </table>
      </div>

      <div style='background:#fef3c7;border:1px solid #fcd34d;border-radius:8px;
                  padding:14px 18px;margin-top:18px;font-size:.82rem;color:#92400e;'>
        <strong>&#9888; Important:</strong> Your License Key is sensitive.
        Never share it with anyone outside of the authorised Emeditech Plus LLP team.
        If hardware changes are required, use the same key in the Re-register Hardware wizard.
      </div>
    </div>

    <!-- Footer -->
    <div style='background:#f9fafb;border-top:1px solid #e5e7eb;padding:18px 36px;
                text-align:center;font-size:.76rem;color:#9ca3af;'>
      This email was sent automatically by <strong>eLUX Stay License Management</strong>.<br/>
      &copy; Emeditech Plus LLP. For support contact your vendor.
    </div>

  </div>
</body>
</html>";
    }

    private static string BuildHardwareRenewalEmailHtml(
        string clientCode, string clientName, string appUrl,
        string macId, string hddSerial, string mbSerial)
    {
        string Row(string label, string? value) =>
            $"<tr><td style='color:#6b7280;padding:7px 0;font-size:.875rem;width:42%;'>{label}</td>" +
            $"<td style='color:#111827;font-weight:600;padding:7px 0;font-size:.875rem;'>{value ?? "—"}</td></tr>";

        var updatedAt = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/><meta name='viewport' content='width=device-width'/></head>
<body style='font-family:""Segoe UI"",Arial,sans-serif;background:#f3f4f6;padding:24px;margin:0;'>
  <div style='max-width:580px;margin:auto;background:#fff;border-radius:12px;
              box-shadow:0 4px 16px rgba(0,0,0,.08);overflow:hidden;'>

    <!-- Header -->
    <div style='background:linear-gradient(135deg,#1e40af,#3b82f6);padding:32px 36px;'>
      <p style='margin:0 0 4px;color:#bfdbfe;font-size:.78rem;letter-spacing:.8px;
                text-transform:uppercase;'>eLUX Stay &mdash; License Management</p>
      <h1 style='margin:0;color:#fff;font-size:1.45rem;font-weight:700;'>Hardware Re-registered</h1>
      <p style='margin:8px 0 0;color:#bfdbfe;font-size:.9rem;'>Your server hardware has been updated and re-validated.</p>
    </div>

    <!-- Body -->
    <div style='padding:28px 36px;'>
      <p style='color:#374151;font-size:.9rem;margin-top:0;'>
        Dear <strong>{clientName}</strong>,<br/>
        The hardware identifiers registered for your <strong>eLUX Stay</strong> license
        have been successfully updated. The system validated the new hardware against
        the central license server and the login page is now accessible.
      </p>

      <!-- Account Details -->
      <div style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;
                  padding:18px 20px;margin:18px 0;'>
        <p style='margin:0 0 10px;font-size:.7rem;font-weight:700;letter-spacing:.7px;
                  text-transform:uppercase;color:#1e40af;'>Account</p>
        <table style='width:100%;border-collapse:collapse;'>
          {Row("Client Code",     clientCode)}
          {Row("Application URL", appUrl)}
          {Row("Updated At",      updatedAt)}
        </table>
      </div>

      <!-- New Hardware -->
      <div style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;
                  padding:18px 20px;margin:18px 0;'>
        <p style='margin:0 0 10px;font-size:.7rem;font-weight:700;letter-spacing:.7px;
                  text-transform:uppercase;color:#1e40af;'>Updated Hardware Identifiers</p>
        <table style='width:100%;border-collapse:collapse;'>
          {Row("MAC Address",      macId)}
          {Row("Hard Disk Serial", hddSerial)}
          {Row("Motherboard ID",   mbSerial)}
        </table>
      </div>

      <div style='background:#fee2e2;border:1px solid #fca5a5;border-radius:8px;
                  padding:14px 18px;margin-top:18px;font-size:.82rem;color:#7f1d1d;'>
        <strong>&#9888; Security Notice:</strong> If you did <em>not</em> authorise this
        hardware change, please contact <strong>Emeditech Plus LLP</strong> immediately
        to suspend your license.
      </div>
    </div>

    <!-- Footer -->
    <div style='background:#f9fafb;border-top:1px solid #e5e7eb;padding:18px 36px;
                text-align:center;font-size:.76rem;color:#9ca3af;'>
      This email was sent automatically by <strong>eLUX Stay License Management</strong>.<br/>
      &copy; Emeditech Plus LLP. For support contact your vendor.
    </div>

  </div>
</body>
</html>";
    }

    // ─── Private DTOs ─────────────────────────────────────────────────────────

    private sealed class SmtpConfig
    {
        public string SmtpServer   { get; set; } = string.Empty;
        public int    SmtpPort     { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool   EnableSSL    { get; set; }
        public string FromEmail    { get; set; } = string.Empty;
        public string? FromName    { get; set; }
    }

    private sealed class OtpRecord
    {
        public long     Id          { get; set; }
        public string   OTP         { get; set; } = string.Empty;
        public bool     isValidated  { get; set; }
        public DateTime Createdate  { get; set; }
    }
}
