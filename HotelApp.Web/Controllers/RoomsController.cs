using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers;

[Authorize]
public class RoomsController : Controller
{
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Room Dashboard";
        return View();
    }
}
