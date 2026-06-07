using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace BloodDonationManagement.Controllers
{
    public class ThanaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThanaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Thana
        public async Task<IActionResult> Index()
        {
            var thanas = await _context.Thanas
                .Include(t => t.District)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View(thanas);
        }

        // GET: Thana/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var thana = await _context.Thanas
                .Include(t => t.District)
                .FirstOrDefaultAsync(m => m.ThanaId == id);
            if (thana == null) return NotFound();

            return View(thana);
        }

        // GET: Thana/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name");
            return View();
        }

        // POST: Thana/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ThanaId,Name,DistrictId")] Thana thana)
        {
            if (ModelState.IsValid)
            {
                _context.Add(thana);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", thana.DistrictId);
            return View(thana);
        }

        // GET: Thana/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var thana = await _context.Thanas.FindAsync(id);
            if (thana == null) return NotFound();

            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", thana.DistrictId);
            return View(thana);
        }

        // POST: Thana/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ThanaId,Name,DistrictId")] Thana thana)
        {
            if (id != thana.ThanaId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(thana);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThanaExists(thana.ThanaId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DistrictId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", thana.DistrictId);
            return View(thana);
        }

        // GET: Thana/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var thana = await _context.Thanas
                .Include(t => t.District)
                .FirstOrDefaultAsync(m => m.ThanaId == id);
            if (thana == null) return NotFound();

            return View(thana);
        }

        // POST: Thana/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var thana = await _context.Thanas.FindAsync(id);
            if (thana != null)
            {
                _context.Thanas.Remove(thana);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ThanaExists(int id)
        {
            return _context.Thanas.Any(e => e.ThanaId == id);
        }
    }
}
