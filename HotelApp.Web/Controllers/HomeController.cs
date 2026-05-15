using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;

namespace HotelApp.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = exceptionFeature?.Error;

        var vm = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ExceptionType = ex?.GetType().Name,
            IsDatabaseError = ex is Microsoft.Data.SqlClient.SqlException
                           || ex?.InnerException is Microsoft.Data.SqlClient.SqlException
                           || ex?.Message.Contains("transport", StringComparison.OrdinalIgnoreCase) == true
                           || ex?.Message.Contains("timeout",   StringComparison.OrdinalIgnoreCase) == true
                           || ex?.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) == true
        };

        return View(vm);
    }
}
