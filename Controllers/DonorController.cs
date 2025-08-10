using BloodDonationManagement.Models;
using BloodDonationManagement.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BloodDonationManagement.Controllers
{
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Donor
        public async Task<IActionResult> Index()
        {
            var donors = await _context.Donors.Include(d => d.District).Include(d => d.Thana).ToListAsync();
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

            return View(donor);
        }

        // GET: Donor/Create
        public IActionResult Create()
        {
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts, "DistrictId", "Name");
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas, "ThanaId", "Name");
            return View();
        }

        // POST: Donor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Donor donor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(donor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts, "DistrictId", "Name", donor.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas, "ThanaId", "Name", donor.ThanaId);
            return View(donor);
        }

        // GET: Donor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var donor = await _context.Donors.FindAsync(id);
            if (donor == null) return NotFound();

            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts, "DistrictId", "Name", donor.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas, "ThanaId", "Name", donor.ThanaId);
            return View(donor);
        }

        // POST: Donor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Donor donor)
        {
            if (id != donor.DonorId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(donor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonorExists(donor.DonorId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts, "DistrictId", "Name", donor.DistrictId);
            ViewData["ThanaId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Thanas, "ThanaId", "Name", donor.ThanaId);
            return View(donor);
        }

        // GET: Donor/Delete/5
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

        // POST: Donor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donor = await _context.Donors.FindAsync(id);
            _context.Donors.Remove(donor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DonorExists(int id)
        {
            return _context.Donors.Any(e => e.DonorId == id);
        }
    }
}
