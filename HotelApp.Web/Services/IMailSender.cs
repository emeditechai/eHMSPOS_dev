namespace HotelApp.Web.Services
{
    public interface IMailSender
    {
        Task SendTestEmailAsync(int branchId, string toEmail);

        Task SendEmailAsync(int branchId, string toEmail, string subject, string htmlBody);
    }
}
