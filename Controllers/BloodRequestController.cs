using BloodDonationManagement.Models;
using BloodDonationManagement.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BloodDonationManagement.Controllers
{
    public class BloodRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BloodRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BloodRequest
        public async Task<IActionResult> Index()
        {
            var requests = await _context.BloodRequests.Include(r => r.District).Include(r => r.Thana).ToListAsync();
            return View(requests);
        }

        // GET: BloodRequest/Details/5
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

        // GET: BloodRequest/Create
        public IActionResult Create()
        {
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts, "DistrictId", "Name");
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas, "ThanaId", "Name");
            return View();
        }

        // POST: BloodRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BloodRequest request)
        {
            if (ModelState.IsValid)
            {
                _context.Add(request);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts, "DistrictId", "Name", request.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas, "ThanaId", "Name", request.ThanaId);
            return View(request);
        }

        // GET: BloodRequest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();

            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts, "DistrictId", "Name", request.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas, "ThanaId", "Name", request.ThanaId);
            return View(request);
        }

        // POST: BloodRequest/Edit/5
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
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BloodRequestExists(request.BloodRequestId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts, "DistrictId", "Name", request.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas, "ThanaId", "Name", request.ThanaId);
            return View(request);
        }

        // GET: BloodRequest/Delete/5
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

        // POST: BloodRequest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            _context.BloodRequests.Remove(request);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BloodRequestExists(int id)
        {
            return _context.BloodRequests.Any(e => e.BloodRequestId == id);
        }
    }
}
