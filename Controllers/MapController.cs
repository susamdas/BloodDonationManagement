using Microsoft.AspNetCore.Mvc;

namespace BloodDonationManagement.Controllers;

public class MapController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Live Blood Map";
        return View();
    }
}
