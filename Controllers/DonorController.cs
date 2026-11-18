using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using BloodDonationManagement.Services;
using BloodDonationManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers
{
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EligibilityService _eligibility;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditService _audit;

        public DonorController(ApplicationDbContext context, EligibilityService eligibility,
                               UserManager<IdentityUser> userManager, AuditService audit)
        {
            _context = context;
            _eligibility = eligibility;
            _userManager = userManager;
            _audit = audit;
        }

        private string? CurrentUserId => _userManager.GetUserId(User);
        private bool IsAdmin => User.IsInRole("Admin");
        private bool IsOwner(Donor d) => !string.IsNullOrEmpty(d.UserId) && d.UserId == CurrentUserId;

        // GET: Donor
        public async Task<IActionResult> Index(string? bloodGroup, int? districtId, string? status, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.Donors
                .Include(d => d.District)
                .Include(d => d.Thana)
                .AsQueryable();

            // Hidden profiles and deactivated accounts are visible to admins only
            if (!IsAdmin)
                query = query.Where(d => !d.IsHidden && d.Status);

            if (!string.IsNullOrEmpty(bloodGroup))
                query = query.Where(d => d.BloodGroup == bloodGroup);

            if (districtId.HasValue)
                query = query.Where(d => d.DistrictId == districtId);

            if (status == "active")
                query = query.Where(d => d.Status && d.IsAvailable);
            else if (status == "inactive")
                query = query.Where(d => !d.Status || !d.IsAvailable);

            var totalCount = await query.CountAsync();

            var donors = await query
                .OrderBy(d => d.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.BloodGroup = bloodGroup;
            ViewBag.DistrictId = districtId;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.ActiveCount = await query.CountAsync(d => d.Status && d.IsAvailable);
            ViewBag.InactiveCount = await query.CountAsync(d => !d.Status || !d.IsAvailable);
            ViewBag.RecentCount = await query.CountAsync(d => d.LastDonationDate >= DateTime.Now.AddDays(-90));
            ViewBag.Districts = new SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", districtId);
            ViewBag.ShowContacts = IsAdmin;

            return View(donors);
        }

        // GET: Donor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var donor = await _context.Donors
                .Include(d => d.District)
                .Include(d => d.Thana)
                .FirstOrDefaultAsync(m => m.DonorId == id);
            if (donor == null) return NotFound();
            if ((donor.IsHidden || !donor.Status) && !IsAdmin && !IsOwner(donor)) return NotFound();

            ViewBag.ShowContact = IsAdmin || IsOwner(donor);
            ViewBag.CanEdit = IsAdmin || IsOwner(donor);
            ViewBag.Eligibility = _eligibility.Evaluate(donor);
            return View(donor);
        }

        // ------------------------------------------------------------------ My donor profile

        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var donor = await _context.Donors
                .Include(d => d.District)
                .Include(d => d.Thana)
                .Include(d => d.Donations).ThenInclude(x => x.BloodRequest)
                .FirstOrDefaultAsync(d => d.UserId == CurrentUserId);

            if (donor == null)
            {
                TempData["InfoMessage"] = "You don't have a donor profile yet. Create one to start saving lives!";
                return RedirectToAction(nameof(Create));
            }

            var responses = await _context.DonorResponses
                .Include(r => r.BloodRequest).ThenInclude(b => b!.District)
                .Where(r => r.DonorId == donor.DonorId)
                .OrderByDescending(r => r.NotifiedAt)
                .Take(20)
                .ToListAsync();

            var notified = await _context.DonorResponses.CountAsync(r => r.DonorId == donor.DonorId);
            var accepted = await _context.DonorResponses.CountAsync(r => r.DonorId == donor.DonorId
                && (r.Status == ResponseStatus.Accepted || r.Status == ResponseStatus.Confirmed));

            var vm = new MyDonorProfileViewModel
            {
                Donor = donor,
                Eligibility = _eligibility.Evaluate(donor),
                Donations = donor.Donations.OrderByDescending(d => d.DonationDate).ToList(),
                Responses = responses,
                TimesNotified = notified,
                TimesAccepted = accepted,
                PendingRequests = responses.Where(r => r.Status == ResponseStatus.Notified && r.BloodRequest!.IsOpen).ToList()
            };
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability()
        {
            var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == CurrentUserId);
            if (donor == null) return RedirectToAction(nameof(Create));
            donor.IsAvailable = !donor.IsAvailable;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = donor.IsAvailable
                ? "You are available again — you will receive matching requests."
                : "Alerts paused. You will not be matched until you switch back on.";
            return RedirectToAction(nameof(MyProfile));
        }

        // ------------------------------------------------------------------ Create

        [Authorize]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin && await _context.Donors.AnyAsync(d => d.UserId == CurrentUserId))
                return RedirectToAction(nameof(MyProfile));

            LoadLocationLists(null, null);
            return View(new Donor { IsAvailable = true, Status = true });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Donor donor)
        {
            var hasProfile = await _context.Donors.AnyAsync(d => d.UserId == CurrentUserId);
            if (!IsAdmin && hasProfile)
                return RedirectToAction(nameof(MyProfile));

            if (ModelState.IsValid)
            {
                donor.BloodGroup = donor.BloodGroup.Trim().ToUpperInvariant();
                donor.CreatedAt = DateTime.Now;
                // Link the profile to the logged-in user (admins may add extra, unlinked donors)
                donor.UserId = hasProfile ? null : CurrentUserId;
                if (!IsAdmin) donor.Status = true;

                _context.Add(donor);
                await _context.SaveChangesAsync();

                if (donor.LastDonationDate.HasValue)
                {
                    _context.Donations.Add(new Donation { DonorId = donor.DonorId, DonationDate = donor.LastDonationDate.Value });
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Donor profile for {donor.FullName} saved.";
                return donor.UserId == CurrentUserId ? RedirectToAction(nameof(MyProfile)) : RedirectToAction(nameof(Index));
            }
            LoadLocationLists(donor.DistrictId, donor.ThanaId);
            return View(donor);
        }

        // ------------------------------------------------------------------ Edit (owner or admin)

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var donor = await _context.Donors.FindAsync(id);
            if (donor == null) return NotFound();
            if (!IsAdmin && !IsOwner(donor)) return Forbid();

            LoadLocationLists(donor.DistrictId, donor.ThanaId);
            return View(donor);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Donor input)
        {
            if (id != input.DonorId) return NotFound();

            var donor = await _context.Donors.FindAsync(id);
            if (donor == null) return NotFound();
            if (!IsAdmin && !IsOwner(donor)) return Forbid();

            if (!ModelState.IsValid)
            {
                LoadLocationLists(input.DistrictId, input.ThanaId);
                return View(input);
            }

            // Copy editable fields only (UserId and CreatedAt are never overwritten)
            var lastDateChanged = input.LastDonationDate?.Date != donor.LastDonationDate?.Date;
            donor.FullName = input.FullName;
            donor.BloodGroup = input.BloodGroup.Trim().ToUpperInvariant();
            donor.Gender = input.Gender;
            donor.ContactNo = input.ContactNo;
            donor.DistrictId = input.DistrictId;
            donor.ThanaId = input.ThanaId;
            donor.Latitude = input.Latitude;
            donor.Longitude = input.Longitude;
            donor.LastDonationDate = input.LastDonationDate;
            donor.IsAvailable = input.IsAvailable;
            donor.IsHidden = input.IsHidden;
            if (IsAdmin) donor.Status = input.Status;

            if (lastDateChanged && input.LastDonationDate.HasValue
                && !await _context.Donations.AnyAsync(d => d.DonorId == id && d.DonationDate == input.LastDonationDate.Value.Date))
            {
                _context.Donations.Add(new Donation { DonorId = id, DonationDate = input.LastDonationDate.Value.Date });
            }

            await _context.SaveChangesAsync();
            if (IsAdmin && !IsOwner(donor))
                await _audit.LogAsync(AuditActions.DonorEditedByAdmin, "Donor", donor.DonorId,
                    $"Admin edited donor {donor.FullName} ({donor.BloodGroup}); active={donor.Status}, available={donor.IsAvailable}");
            TempData["SuccessMessage"] = "Donor profile updated.";
            return IsOwner(donor) ? RedirectToAction(nameof(MyProfile)) : RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------ Delete (admin)

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var donor = await _context.Donors
                .Include(d => d.District)
                .Include(d => d.Thana)
                .FirstOrDefaultAsync(m => m.DonorId == id);
            if (donor == null) return NotFound();

            return View(donor);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donor = await _context.Donors.FindAsync(id);
            if (donor == null) return NotFound();
            var summary = $"Deleted donor {donor.FullName} ({donor.BloodGroup})";
            _context.Donors.Remove(donor);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(AuditActions.DonorDeleted, "Donor", id, summary);
            TempData["SuccessMessage"] = "Donor deleted.";
            return RedirectToAction(nameof(Index));
        }

        private void LoadLocationLists(int? districtId, int? thanaId)
        {
            ViewData["DistrictId"] = new SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", districtId);
            ViewData["ThanaId"] = new SelectList(
                districtId.HasValue ? _context.Thanas.Where(t => t.DistrictId == districtId.Value).OrderBy(t => t.Name) : _context.Thanas.OrderBy(t => t.Name),
                "ThanaId", "Name", thanaId);
        }
    }
}
