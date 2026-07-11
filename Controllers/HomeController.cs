using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using BloodDonationManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var bloodGroupStats = await _context.Donors
            .Where(d => d.Status)
            .GroupBy(d => d.BloodGroup)
            .Select(g => new { BloodGroup = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.BloodGroup, g => g.Count);

        var recentRequests = await _context.BloodRequests
            .Include(r => r.District)
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderByDescending(r => (int)r.Urgency)
            .ThenByDescending(r => r.CreatedDate)
            .Take(5)
            .ToListAsync();

        var viewModel = new DashboardViewModel
        {
            TotalDonors = await _context.Donors.CountAsync(),
            ActiveDonors = await _context.Donors.CountAsync(d => d.Status),
            TotalRequests = await _context.BloodRequests.CountAsync(),
            PendingRequests = await _context.BloodRequests.CountAsync(r => r.Status == RequestStatus.Pending),
            FulfilledRequests = await _context.BloodRequests.CountAsync(r => r.Status == RequestStatus.Fulfilled),
            DistrictCount = await _context.Districts.CountAsync(),
            ThanaCount = await _context.Thanas.CountAsync(),
            BloodGroupStats = bloodGroupStats,
            RecentRequests = recentRequests
        };

        return View(viewModel);
    }

    public IActionResult Privacy() => View();

    public IActionResult Donors() => RedirectToAction("Index", "Donor");
    public IActionResult BloodRequests() => RedirectToAction("Index", "BloodRequest");
    public IActionResult Districts() => RedirectToAction("Index", "District");
}
