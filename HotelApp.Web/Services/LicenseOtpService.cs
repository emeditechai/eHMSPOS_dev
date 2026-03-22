using Dapper;
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
