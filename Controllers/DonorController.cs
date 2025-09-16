using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using BloodDonationManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers
{
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DonorService _donorService;

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

        //GET: Donor/Create
        public IActionResult Create()
        {
            ViewBag.DistrictId = new SelectList(
                _context.Districts.Where(d => d.IsActive == true)
                                  .OrderBy(d => d.District_Name_Eng)
                                  .ToList(),
                "DistrictId",
                "District_Name_Eng"
            );

            // সক্রিয় থানা গুলো নিয়ে dropdown
            ViewBag.ThanaId = new SelectList(_context.Thanas, "ThanaId", "Thana_Name_Eng");

            return View();
        }





        // POST: Donor/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Donor donor)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _donorService.AddDonorAsync(donor);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public JsonResult GetThanasByDistrict(int districtId)
        {
            var thanas = _context.Thanas
                .Where(t => t.DistrictId == districtId)
                .OrderBy(t => t.Thana_Name_Eng)
                .Select(t => new { t.ThanaId, t.Thana_Name_Eng })
                .ToList();

            return Json(thanas);
        }


        // GET: Donor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var donor = await _context.Donors.FindAsync(id);
            if (donor == null) return NotFound();

            ViewBag["DistrictId"] = new SelectList(_context.Districts, "DistrictId", "District_Name_Eng", donor.DistrictId);
            ViewBag["ThanaId"] = new SelectList(_context.Thanas, "ThanaId", "Thana_Name_Eng", donor.ThanaId);
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
            ViewBag["DistrictId"] = new SelectList(_context.Districts, "DistrictId", "District_Name_Eng", donor.DistrictId);
            ViewBag["ThanaId"] = new SelectList(_context.Thanas, "ThanaId", "Thana_Name_Eng", donor.ThanaId);
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
