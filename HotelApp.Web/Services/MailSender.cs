using HotelApp.Web.Repositories;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HotelApp.Web.Services
{
    public class MailSender : IMailSender
    {
        private readonly IMailConfigurationRepository _mailConfigurationRepository;
        private readonly IMailPasswordProtector _passwordProtector;

        public MailSender(
            IMailConfigurationRepository mailConfigurationRepository,
            IMailPasswordProtector passwordProtector)
        {
            _mailConfigurationRepository = mailConfigurationRepository;
            _passwordProtector = passwordProtector;
        }

        public async Task SendTestEmailAsync(int branchId, string toEmail)
        {
            await SendMessageAsync(
                branchId,
                toEmail,
                subject: "Test Email - SMTP Configuration",
                textBody: "This is a test email sent from HotelApp to verify SMTP configuration.",
                htmlBody: null);
        }

        public async Task SendEmailAsync(int branchId, string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new InvalidOperationException("Email subject is required.");
            }

            if (string.IsNullOrWhiteSpace(htmlBody))
            {
                throw new InvalidOperationException("Email body is required.");
            }

            await SendMessageAsync(branchId, toEmail, subject, textBody: null, htmlBody: htmlBody);
        }

        private async Task SendMessageAsync(int branchId, string toEmail, string subject, string? textBody, string? htmlBody)
        {
            var config = await _mailConfigurationRepository.GetByBranchAsync(branchId);
            if (config == null || !config.IsActive)
            {
                throw new InvalidOperationException("Email service is not active for this branch.");
            }

            var host = (config.SmtpHost ?? string.Empty).Trim();
            var port = config.SmtpPort ?? 0;
            var user = (config.UserName ?? string.Empty).Trim();
            var fromEmail = (config.FromEmail ?? string.Empty).Trim();
            var fromName = (config.FromName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
            {
                throw new InvalidOperationException("SMTP server/port is not configured.");
            }

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("From Email is not configured.");
            }

            var password = _passwordProtector.Unprotect(config.PasswordProtected ?? string.Empty);
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("SMTP username/password is not configured.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(string.IsNullOrWhiteSpace(fromName) ? fromEmail : fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            if (!string.IsNullOrWhiteSpace(htmlBody))
            {
                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                if (!string.IsNullOrWhiteSpace(textBody))
                {
                    builder.TextBody = textBody;
                }

                message.Body = builder.ToMessageBody();
            }
            else
            {
                message.Body = new TextPart("plain")
                {
                    Text = textBody ?? string.Empty
                };
            }

            var options = SecureSocketOptions.None;
            if (config.EnableSslTls)
            {
                options = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            }

            using var smtp = new SmtpClient();
            smtp.Timeout = 20000;

            await smtp.ConnectAsync(host, port, options);
            await smtp.AuthenticateAsync(user, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
