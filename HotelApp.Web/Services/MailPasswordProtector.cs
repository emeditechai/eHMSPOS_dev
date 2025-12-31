using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace HotelApp.Web.Services
{
    public class MailPasswordProtector : IMailPasswordProtector
    {
        private readonly IDataProtector _protector;

        public MailPasswordProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("HotelApp.Web.MailConfiguration.Password.v1");
        }

        public string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = _protector.Protect(bytes);
            return Convert.ToBase64String(protectedBytes);
        }

        public string Unprotect(string protectedValue)
        {
            if (string.IsNullOrWhiteSpace(protectedValue))
            {
                return string.Empty;
            }

            var protectedBytes = Convert.FromBase64String(protectedValue);
            var bytes = _protector.Unprotect(protectedBytes);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
