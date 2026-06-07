using BloodDonationManagement.Data;
using BloodDonationManagement.Hubs;
using BloodDonationManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers
{
    public class BloodRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public BloodRequestController(ApplicationDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.BloodRequests
                .Include(r => r.District)
                .Include(r => r.Thana)
                .OrderByDescending(r => r.Urgency)
                .ThenByDescending(r => r.CreatedDate)
                .ToListAsync();
            return View(requests);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.BloodRequests
                .Include(r => r.District)
                .Include(r => r.Thana)
                .FirstOrDefaultAsync(m => m.BloodRequestId == id);
            if (request == null) return NotFound();
            return View(request);
        }

        [Authorize]
        public IActionResult Create()
        {
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name");
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(Enumerable.Empty<Thana>(), "ThanaId", "Name");
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BloodRequest request)
        {
            if (ModelState.IsValid)
            {
                request.CreatedDate = DateTime.Now;
                _context.Add(request);
                await _context.SaveChangesAsync();

                var district = request.DistrictId.HasValue
                    ? await _context.Districts.FindAsync(request.DistrictId.Value)
                    : null;

                await _hub.Clients.All.SendAsync("NewBloodRequest", new
                {
                    request.BloodRequestId,
                    request.BloodGroup,
                    request.PatientName,
                    District = district?.Name ?? "Unknown",
                    Urgency = request.Urgency.ToString(),
                    Hospital = request.HospitalName ?? ""
                });

                TempData["SuccessMessage"] = $"Blood request for {request.BloodGroup} submitted successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", request.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                request.DistrictId.HasValue ? _context.Thanas.Where(t => t.DistrictId == request.DistrictId.Value) : Enumerable.Empty<Thana>(),
                "ThanaId", "Name", request.ThanaId);
            return View(request);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", request.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas.Where(t => t.DistrictId == request.DistrictId), "ThanaId", "Name", request.ThanaId);
            return View(request);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BloodRequest request)
        {
            if (id != request.BloodRequestId) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(request);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Blood request updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BloodRequestExists(request.BloodRequestId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", request.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas.Where(t => t.DistrictId == request.DistrictId), "ThanaId", "Name", request.ThanaId);
            return View(request);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.BloodRequests
                .Include(r => r.District)
                .Include(r => r.Thana)
                .FirstOrDefaultAsync(m => m.BloodRequestId == id);
            if (request == null) return NotFound();
            return View(request);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request != null)
            {
                _context.BloodRequests.Remove(request);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Blood request deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fulfill(int id)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request != null)
            {
                request.Status = "Fulfilled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Blood request marked as fulfilled!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BloodRequestExists(int id) => _context.BloodRequests.Any(e => e.BloodRequestId == id);
    }
}
