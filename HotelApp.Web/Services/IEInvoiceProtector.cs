namespace HotelApp.Web.Services
{
    public interface IEInvoiceProtector
    {
        string Protect(string plainText);
        string Unprotect(string protectedValue);
    }
}
