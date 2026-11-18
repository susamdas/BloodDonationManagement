using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using BloodDonationManagement.Services;
using BloodDonationManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers;

public class HomeController : Controller
{
    private static readonly string[] Groups = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

    private readonly ApplicationDbContext _context;
    private readonly EligibilityService _eligibility;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ApplicationDbContext context, EligibilityService eligibility, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _eligibility = eligibility;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var donors = await _context.Donors.AsNoTracking().Where(d => d.Status).ToListAsync();
        var eligible = donors.Where(d => d.IsAvailable && _eligibility.Evaluate(d).IsEligible).ToList();

        var requests = await _context.BloodRequests.AsNoTracking()
            .Include(r => r.District)
            .Include(r => r.Responses)
            .ToListAsync();
        var open = requests.Where(r => r.IsOpen && (r.NeededBy == null || r.NeededBy > DateTime.Now)).ToList();

        var vm = new DashboardViewModel
        {
            ActiveDonors = donors.Count,
            EligibleNow = eligible.Count,
            OpenRequests = open.Count,
            CriticalOpen = open.Count(r => r.Urgency == UrgencyLevel.Critical),
            FulfilledRequests = requests.Count(r => r.Status == RequestStatus.Fulfilled),
            TotalDonations = await _context.Donations.CountAsync(),
            AvgMinutesToFirstAccept = StatsHelper.AvgMinutesToFirstAccept(requests),
            DistrictsCovered = donors.Where(d => d.DistrictId.HasValue).Select(d => d.DistrictId).Distinct().Count(),
            EligibleByGroup = Groups.ToDictionary(g => g, g => eligible.Count(d => d.BloodGroup == g)),
            UrgentRequests = open
                .OrderByDescending(r => r.Urgency)
                .ThenBy(r => r.NeededBy ?? DateTime.MaxValue)
                .ThenByDescending(r => r.CreatedDate)
                .Take(4)
                .ToList(),
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            IsAdmin = User.IsInRole("Admin")
        };

        if (vm.IsAuthenticated)
        {
            var userId = _userManager.GetUserId(User);
            vm.MyDonor = donors.FirstOrDefault(d => d.UserId == userId)
                         ?? await _context.Donors.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId);
            if (vm.MyDonor != null)
            {
                vm.MyEligibility = _eligibility.Evaluate(vm.MyDonor);
                vm.MyPendingAlerts = await _context.DonorResponses.CountAsync(r =>
                    r.DonorId == vm.MyDonor.DonorId && r.Status == ResponseStatus.Notified
                    && (r.BloodRequest!.Status == RequestStatus.Pending || r.BloodRequest.Status == RequestStatus.DonorMatched));
            }
            vm.MyOpenRequests = open.Count(r => r.RequesterUserId == userId);
            if (vm.IsAdmin)
                vm.OpenReports = await _context.RequestReports.CountAsync(r => r.Status == ReportStatus.Open);
        }

        return View(vm);
    }

    public IActionResult Privacy() => View();

    public IActionResult Donors() => RedirectToAction("Index", "Donor");
    public IActionResult BloodRequests() => RedirectToAction("Index", "BloodRequest");
    public IActionResult Districts() => RedirectToAction("Index", "District");
}
