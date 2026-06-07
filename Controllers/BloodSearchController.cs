using BloodDonationManagement.Data;
using BloodDonationManagement.Services;
using BloodDonationManagement.ViewModels;
using BloodDonationManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers;

public class BloodSearchController : Controller
{
    private readonly ApplicationDbContext _context;

    public BloodSearchController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(
        string? bloodGroup,
        int? districtId,
        int? thanaId,
        bool compatibilitySearch = false,
        double? userLat = null,
        double? userLng = null,
        double radiusKm = 20)
    {
        var query = _context.Donors
            .Include(d => d.District)
            .Include(d => d.Thana)
            .Where(d => d.Status)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(bloodGroup))
        {
            var group = bloodGroup.Trim().ToUpper();
            if (compatibilitySearch)
            {
                var compatible = BloodCompatibilityService.GetCompatibleDonorGroups(group);
                query = query.Where(d => compatible.Contains(d.BloodGroup));
            }
            else
            {
                query = query.Where(d => d.BloodGroup == group);
            }
        }

        if (districtId.HasValue)
            query = query.Where(d => d.DistrictId == districtId.Value);

        if (thanaId.HasValue)
            query = query.Where(d => d.ThanaId == thanaId.Value);

        var donors = await query.OrderBy(d => d.FullName).ToListAsync();

        var resultsWithDistance = donors.Select(d => new DonorWithDistance
        {
            Donor = d,
            DistanceKm = (userLat.HasValue && userLng.HasValue && d.Latitude.HasValue && d.Longitude.HasValue)
                ? DistanceService.Haversine(userLat.Value, userLng.Value, d.Latitude.Value, d.Longitude.Value)
                : null
        }).ToList();

        if (userLat.HasValue && userLng.HasValue)
        {
            resultsWithDistance = resultsWithDistance
                .Where(r => r.DistanceKm == null || r.DistanceKm <= radiusKm)
                .OrderBy(r => r.DistanceKm ?? double.MaxValue)
                .ToList();
        }

        var model = new BloodSearchViewModel
        {
            BloodGroup = bloodGroup,
            DistrictId = districtId,
            ThanaId = thanaId,
            CompatibilitySearch = compatibilitySearch,
            UserLat = userLat,
            UserLng = userLng,
            RadiusKm = radiusKm,
            ResultsWithDistance = resultsWithDistance,
            BloodGroups =
            [
                new("A+", "A+"), new("A-", "A-"),
                new("B+", "B+"), new("B-", "B-"),
                new("AB+", "AB+"), new("AB-", "AB-"),
                new("O+", "O+"), new("O-", "O-")
            ],
            Districts = await _context.Districts
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem(d.Name, d.DistrictId.ToString()))
                .ToListAsync()
        };

        if (districtId.HasValue)
        {
            model.Thanas = await _context.Thanas
                .Where(t => t.DistrictId == districtId.Value)
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem(t.Name, t.ThanaId.ToString()))
                .ToListAsync();
        }

        return View(model);
    }
}
