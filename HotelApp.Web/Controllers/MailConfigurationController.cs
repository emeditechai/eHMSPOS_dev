using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.Services;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class MailConfigurationController : BaseController
    {
        private readonly IMailConfigurationRepository _mailConfigurationRepository;
        private readonly IMailPasswordProtector _passwordProtector;
        private readonly IMailSender _mailSender;

        public MailConfigurationController(
            IMailConfigurationRepository mailConfigurationRepository,
            IMailPasswordProtector passwordProtector,
            IMailSender mailSender)
        {
            _mailConfigurationRepository = mailConfigurationRepository;
            _passwordProtector = passwordProtector;
            _mailSender = mailSender;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _mailConfigurationRepository.GetByBranchAsync(CurrentBranchID);
            if (settings == null)
            {
                settings = new MailConfiguration
                {
                    BranchID = CurrentBranchID,
                    SmtpHost = "smtp.gmail.com",
                    SmtpPort = 587,
                    EnableSslTls = true,
                    IsActive = false
                };
            }

            var vm = new MailConfigurationViewModel
            {
                Id = settings.Id,
                BranchID = settings.BranchID,
                SmtpHost = settings.SmtpHost,
                SmtpPort = settings.SmtpPort,
                UserName = settings.UserName,
                HasPassword = !string.IsNullOrWhiteSpace(settings.PasswordProtected),
                EnableSslTls = settings.EnableSslTls,
                FromEmail = settings.FromEmail,
                FromName = settings.FromName,
                AdminNotificationEmail = settings.AdminNotificationEmail,
                IsActive = settings.IsActive
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(MailConfigurationViewModel model)
        {
            model.BranchID = CurrentBranchID;

            var existing = await _mailConfigurationRepository.GetByBranchAsync(CurrentBranchID);
            var hasExistingPassword = existing != null && !string.IsNullOrWhiteSpace(existing.PasswordProtected);

            if (model.IsActive)
            {
                if (string.IsNullOrWhiteSpace(model.SmtpHost))
                {
                    ModelState.AddModelError(nameof(model.SmtpHost), "SMTP Server is required.");
                }

                if (!model.SmtpPort.HasValue || model.SmtpPort.Value <= 0 || model.SmtpPort.Value > 65535)
                {
                    ModelState.AddModelError(nameof(model.SmtpPort), "SMTP Port must be between 1 and 65535.");
                }

                if (string.IsNullOrWhiteSpace(model.UserName))
                {
                    ModelState.AddModelError(nameof(model.UserName), "Username is required.");
                }

                if (!hasExistingPassword && string.IsNullOrWhiteSpace(model.Password))
                {
                    ModelState.AddModelError(nameof(model.Password), "Password is required (first-time setup).");
                }

                if (string.IsNullOrWhiteSpace(model.FromEmail))
                {
                    ModelState.AddModelError(nameof(model.FromEmail), "From Email is required.");
                }
                else if (!model.FromEmail.Contains('@'))
                {
                    ModelState.AddModelError(nameof(model.FromEmail), "From Email must be a valid email address.");
                }

                if (string.IsNullOrWhiteSpace(model.FromName))
                {
                    ModelState.AddModelError(nameof(model.FromName), "From Name is required.");
                }

                if (!string.IsNullOrWhiteSpace(model.AdminNotificationEmail) && !model.AdminNotificationEmail.Contains('@'))
                {
                    ModelState.AddModelError(nameof(model.AdminNotificationEmail), "Admin Notification Email must be a valid email address.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.HasPassword = hasExistingPassword;
                return View(nameof(Index), model);
            }

            try
            {
                var toSave = new MailConfiguration
                {
                    Id = existing?.Id ?? 0,
                    BranchID = CurrentBranchID,
                    SmtpHost = model.SmtpHost?.Trim(),
                    SmtpPort = model.SmtpPort,
                    UserName = model.UserName?.Trim(),
                    EnableSslTls = model.EnableSslTls,
                    FromEmail = model.FromEmail?.Trim(),
                    FromName = model.FromName?.Trim(),
                    AdminNotificationEmail = model.AdminNotificationEmail?.Trim(),
                    IsActive = model.IsActive,
                    CreatedBy = existing?.CreatedBy ?? CurrentUserId,
                    LastModifiedBy = CurrentUserId
                };

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    toSave.PasswordProtected = _passwordProtector.Protect(model.Password.Trim());
                }
                else
                {
                    toSave.PasswordProtected = existing?.PasswordProtected;
                }

                await _mailConfigurationRepository.UpsertAsync(toSave);

                TempData["SuccessMessage"] = "Email configuration saved successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving email configuration: {ex.Message}";
                model.HasPassword = hasExistingPassword;
                return View(nameof(Index), model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTest(MailConfigurationViewModel model)
        {
            var toEmail = (model.TestToEmail ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                var existing = await _mailConfigurationRepository.GetByBranchAsync(CurrentBranchID);
                toEmail = (existing?.AdminNotificationEmail ?? existing?.FromEmail ?? string.Empty).Trim();
            }

            if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains('@'))
            {
                TempData["ErrorMessage"] = "Please enter a valid Test To Email (or configure Admin Notification Email).";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _mailSender.SendTestEmailAsync(CurrentBranchID, toEmail);
                TempData["SuccessMessage"] = $"Test email sent successfully to {toEmail}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = GetFriendlySendTestError(ex);
            }

            return RedirectToAction(nameof(Index));
        }

        private static string GetFriendlySendTestError(Exception ex)
        {
            var combined = (ex.ToString() ?? string.Empty) + "\n" + (ex.InnerException?.ToString() ?? string.Empty);

            // Gmail commonly blocks basic auth with normal password and requires an App Password.
            // Typical errors include: 534 5.7.9 Application-specific password required, InvalidSecondFactor.
            if (combined.Contains("5.7.9", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains("Application-specific password", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains("InvalidSecondFactor", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains("Please log in via your web browser", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed to send test email: Gmail blocked SMTP login. Use a Gmail App Password (enable 2‑Step Verification, then generate an App Password and use that here). Normal Gmail password will not work.";
            }

            if (combined.Contains("Authentication", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains("535", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains("Username and Password not accepted", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed to send test email: SMTP authentication failed. Verify username/password, and for Gmail/Office365 prefer using an App Password / SMTP-auth enabled account.";
            }

            if (combined.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed to send test email: connection timed out. Check SMTP host/port, firewall, and internet connectivity.";
            }

            return $"Failed to send test email: {ex.Message}";
        }
    }
}
