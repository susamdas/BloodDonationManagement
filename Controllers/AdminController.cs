using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using BloodDonationManagement.Services;
using BloodDonationManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private static readonly string[] Groups = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly EligibilityService _eligibility;
    private readonly NotificationService _notifications;

    public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager,
                           EligibilityService eligibility, NotificationService notifications)
    {
        _context = context;
        _userManager = userManager;
        _eligibility = eligibility;
        _notifications = notifications;
    }

    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    // ================================================================== Analytics (FR-10)

    public async Task<IActionResult> Dashboard()
    {
        var donors = await _context.Donors.AsNoTracking().Where(d => d.Status).ToListAsync();
        var eligibleDonors = donors.Where(d => d.IsAvailable && _eligibility.Evaluate(d).IsEligible).ToList();

        var requests = await _context.BloodRequests.AsNoTracking()
            .Include(r => r.District)
            .Include(r => r.Responses)
            .ToListAsync();

        var vm = new AnalyticsViewModel
        {
            ActiveDonors = donors.Count,
            EligibleNow = eligibleDonors.Count,
            RareEligible = eligibleDonors.Count(d => DonorMatchingService.IsRareGroup(d.BloodGroup)),
            OpenRequests = requests.Count(r => r.IsOpen),
            CriticalOpen = requests.Count(r => r.IsOpen && r.Urgency == UrgencyLevel.Critical),
            FulfilledRequests = requests.Count(r => r.Status == RequestStatus.Fulfilled),
            TotalDonations = await _context.Donations.CountAsync(),
            OpenReports = await _context.RequestReports.CountAsync(r => r.Status == ReportStatus.Open)
        };

        // Fulfilment rate: fulfilled / requests that reached an outcome (cancelled ones excluded)
        var decided = requests.Count(r => r.Status is RequestStatus.Fulfilled or RequestStatus.Expired);
        vm.FulfilmentRate = decided == 0 ? 0 : (int)Math.Round(100.0 * vm.FulfilledRequests / decided);

        // Average time from request creation to the first donor saying yes (key performance metric)
        var firstAccepts = requests
            .Select(r => new
            {
                r.CreatedDate,
                First = r.Responses
                    .Where(x => x.RespondedAt.HasValue && (x.Status == ResponseStatus.Accepted || x.Status == ResponseStatus.Confirmed))
                    .Select(x => x.RespondedAt!.Value)
                    .DefaultIfEmpty()
                    .Min()
            })
            .Where(x => x.First != default && x.First >= x.CreatedDate)
            .Select(x => (x.First - x.CreatedDate).TotalMinutes)
            .ToList();
        vm.AvgMinutesToFirstAccept = firstAccepts.Count == 0 ? null : Math.Round(firstAccepts.Average(), 1);

        var allResponses = requests.SelectMany(r => r.Responses).ToList();
        var answered = allResponses.Count;
        var yes = allResponses.Count(x => x.Status is ResponseStatus.Accepted or ResponseStatus.Confirmed);
        vm.AcceptanceRate = answered == 0 ? 0 : (int)Math.Round(100.0 * yes / answered);

        // Supply (eligible donors) vs demand (requests in the last 90 days) per blood group
        var since = DateTime.Now.AddDays(-90);
        vm.BloodGroups = Groups.ToList();
        vm.EligibleByGroup = Groups.Select(g => eligibleDonors.Count(d => d.BloodGroup == g)).ToList();
        vm.RequestsByGroup = Groups.Select(g => requests.Count(r => r.BloodGroup == g && r.CreatedDate >= since)).ToList();

        // 30-day trend
        var start = DateTime.Today.AddDays(-29);
        for (var day = start; day <= DateTime.Today; day = day.AddDays(1))
        {
            var d = day;
            vm.TrendDays.Add(d.ToString("dd MMM"));
            vm.TrendCreated.Add(requests.Count(r => r.CreatedDate.Date == d));
            vm.TrendFulfilled.Add(requests.Count(r => r.FulfilledAt.HasValue && r.FulfilledAt.Value.Date == d && r.Status == RequestStatus.Fulfilled));
        }

        vm.ByUrgency = Enum.GetValues<UrgencyLevel>()
            .Select(u => new LabelValue(u.ToString(), requests.Count(r => r.Urgency == u)))
            .ToList();

        vm.TopDistricts = requests
            .Where(r => r.District != null)
            .GroupBy(r => r.District!.Name)
            .Select(g => new LabelValue(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .Take(6)
            .ToList();

        // Most reliable donors (at least 2 alerts)
        var donationCounts = await _context.Donations.GroupBy(d => d.DonorId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var donorById = donors.ToDictionary(d => d.DonorId);
        vm.TopDonors = allResponses
            .GroupBy(x => x.DonorId)
            .Where(g => g.Count() >= 2 && donorById.ContainsKey(g.Key))
            .Select(g =>
            {
                var d = donorById[g.Key];
                var rate = (int)Math.Round(100.0 * g.Count(x => x.Status is ResponseStatus.Accepted or ResponseStatus.Confirmed) / g.Count());
                return new ReliableDonor(d.DonorId, d.FullName, d.BloodGroup, donationCounts.GetValueOrDefault(d.DonorId), rate);
            })
            .OrderByDescending(x => x.ResponseRate).ThenByDescending(x => x.Donations)
            .Take(5)
            .ToList();

        return View(vm);
    }

    // ================================================================== User management (FR-6.5)

    public async Task<IActionResult> Users(string? search)
    {
        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Email!.Contains(search) || (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));

        var users = await query.OrderBy(u => u.Email).Take(200).ToListAsync();
        var admins = (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Id).ToHashSet();
        var ids = users.Select(u => u.Id).ToList();
        var donors = await _context.Donors.Where(d => d.UserId != null && ids.Contains(d.UserId)).ToListAsync();
        var requestCounts = await _context.BloodRequests
            .Where(r => r.RequesterUserId != null && ids.Contains(r.RequesterUserId))
            .GroupBy(r => r.RequesterUserId!)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var vm = new UsersViewModel
        {
            Search = search,
            CurrentUserId = _userManager.GetUserId(User),
            Users = users.Select(u => new UserRow
            {
                Id = u.Id,
                Email = u.Email ?? u.UserName ?? "",
                Phone = u.PhoneNumber,
                IsAdmin = admins.Contains(u.Id),
                IsActive = !(u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(50)),
                Donor = donors.FirstOrDefault(d => d.UserId == u.Id),
                Requests = requestCounts.GetValueOrDefault(u.Id)
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        if (id == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Users));
        }
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var deactivate = !(user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(50));
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, deactivate ? DateTimeOffset.MaxValue : null);
        await _userManager.UpdateSecurityStampAsync(user); // signs the user out everywhere

        // Deactivated users are removed from matching too
        var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
        if (donor != null)
        {
            donor.Status = !deactivate;
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = deactivate ? $"{user.Email} has been deactivated." : $"{user.Email} has been re-activated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        if (id == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "You cannot change your own admin role.";
            return RedirectToAction(nameof(Users));
        }
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            TempData["SuccessMessage"] = $"{user.Email} is no longer an admin.";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["SuccessMessage"] = $"{user.Email} is now an admin.";
        }
        await _userManager.UpdateSecurityStampAsync(user);
        return RedirectToAction(nameof(Users));
    }

    // ================================================================== Moderation (FR-6.2 / 6.3)

    public async Task<IActionResult> Reports(string? show)
    {
        var query = _context.RequestReports
            .Include(r => r.BloodRequest).ThenInclude(b => b!.District)
            .AsQueryable();
        if (show != "all") query = query.Where(r => r.Status == ReportStatus.Open);

        var reports = await query.OrderBy(r => r.Status).ThenByDescending(r => r.CreatedAt).Take(200).ToListAsync();

        var reporterIds = reports.Select(r => r.ReporterUserId).Distinct().ToList();
        ViewBag.Reporters = await _userManager.Users.Where(u => reporterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? "");
        ViewBag.Show = show;
        return View(reports);
    }

    /// <param name="decision">dismiss | verify | remove</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveReport(int id, string decision)
    {
        var report = await _context.RequestReports.Include(r => r.BloodRequest).FirstOrDefaultAsync(r => r.RequestReportId == id);
        if (report?.BloodRequest == null) return NotFound();
        var request = report.BloodRequest;

        // Resolve every open report for the same request at once
        var related = await _context.RequestReports
            .Where(r => r.BloodRequestId == request.BloodRequestId && r.Status == ReportStatus.Open)
            .ToListAsync();

        switch (decision)
        {
            case "remove":
                if (request.IsOpen)
                {
                    request.Status = RequestStatus.Cancelled;
                    request.FulfilledAt = DateTime.Now;
                }
                request.IsVerified = false;
                related.ForEach(r => r.Status = ReportStatus.ActionTaken);
                await _context.SaveChangesAsync();
                await _notifications.NotifyAsync(request.RequesterUserId, "Your request was removed",
                    "An administrator closed your blood request after reviewing user reports. Contact support if this is a mistake.",
                    Url.Action("Details", "BloodRequest", new { id = request.BloodRequestId }), "warning");
                TempData["SuccessMessage"] = "Request closed and reports resolved.";
                break;
            case "verify":
                request.IsVerified = true;
                related.ForEach(r => r.Status = ReportStatus.Dismissed);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Request verified as genuine; reports dismissed.";
                break;
            default:
                related.ForEach(r => r.Status = ReportStatus.Dismissed);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Reports dismissed.";
                break;
        }
        return RedirectToAction(nameof(Reports));
    }
}
