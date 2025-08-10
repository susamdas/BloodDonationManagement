using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();  // Views/Home/Index.cshtml ???? ??????? ????
    }

    public IActionResult Donors()
    {
        // ????? ????? ???
        return RedirectToAction("Index", "Donor"); // DonorController ?? Index Action ? ????
    }

    public IActionResult BloodRequests()
    {
        return RedirectToAction("Index", "BloodRequest");
    }

    public IActionResult Districts()
    {
        return RedirectToAction("Index", "District");
    }
}
