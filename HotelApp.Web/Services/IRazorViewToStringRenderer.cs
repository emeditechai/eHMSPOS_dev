using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Services
{
    public interface IRazorViewToStringRenderer
    {
        Task<string> RenderViewToStringAsync(Controller controller, string viewName, object model);
    }
}
