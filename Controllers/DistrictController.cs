using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers
{
    public class DistrictController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DistrictController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: District
        public async Task<IActionResult> Index()
        {
            var districts = await _context.Districts
                .Include(d => d.Thanas)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(districts);
        }

        // GET: District/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var district = await _context.Districts
                .Include(d => d.Thanas)
                .FirstOrDefaultAsync(m => m.DistrictId == id);
            if (district == null) return NotFound();

            return View(district);
        }

        // GET: District/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: District/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DistrictId,Name")] District district)
        {
            if (ModelState.IsValid)
            {
                _context.Add(district);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(district);
        }

        // GET: District/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var district = await _context.Districts.FindAsync(id);
            if (district == null) return NotFound();

            return View(district);
        }

        // POST: District/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DistrictId,Name")] District district)
        {
            if (id != district.DistrictId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(district);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DistrictExists(district.DistrictId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(district);
        }

        // GET: District/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var district = await _context.Districts
                .FirstOrDefaultAsync(m => m.DistrictId == id);
            if (district == null) return NotFound();

            return View(district);
        }

        // POST: District/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var district = await _context.Districts.FindAsync(id);
            if (district != null)
            {
                _context.Districts.Remove(district);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetThanasByDistrict(int districtId)
        {
            var thanas = await _context.Thanas
                .Where(t => t.DistrictId == districtId)
                .OrderBy(t => t.Name)
                .Select(t => new { value = t.ThanaId, text = t.Name })
                .ToListAsync();

            return Json(thanas);
        }

        private bool DistrictExists(int id)
        {
            return _context.Districts.Any(e => e.DistrictId == id);
        }
    }
}
