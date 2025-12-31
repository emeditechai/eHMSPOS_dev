namespace HotelApp.Web.Services
{
    public interface IMailPasswordProtector
    {
        string Protect(string plainText);
        string Unprotect(string protectedValue);
    }
}
