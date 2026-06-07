using BloodDonationManagement.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MapDataController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MapDataController(ApplicationDbContext context) => _context = context;

    [HttpGet("donors")]
    public async Task<IActionResult> GetDonors([FromQuery] string? bloodGroup)
    {
        var query = _context.Donors
            .Include(d => d.District)
            .Include(d => d.Thana)
            .Where(d => d.Status && d.Latitude.HasValue && d.Longitude.HasValue);

        if (!string.IsNullOrWhiteSpace(bloodGroup))
            query = query.Where(d => d.BloodGroup == bloodGroup.ToUpper());

        var donors = await query.Select(d => new
        {
            d.DonorId,
            d.FullName,
            d.BloodGroup,
            d.ContactNo,
            d.Latitude,
            d.Longitude,
            District = d.District != null ? d.District.Name : "",
            Thana = d.Thana != null ? d.Thana.Name : "",
            LastDonation = d.LastDonationDate != null ? d.LastDonationDate.Value.ToString("yyyy-MM-dd") : ""
        }).ToListAsync();

        return Ok(donors);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        var requests = await _context.BloodRequests
            .Include(r => r.District)
            .Include(r => r.Thana)
            .Where(r => r.Status == "Pending" && r.Latitude.HasValue && r.Longitude.HasValue)
            .Select(r => new
            {
                r.BloodRequestId,
                r.PatientName,
                r.BloodGroup,
                r.ContactNo,
                r.Latitude,
                r.Longitude,
                r.Urgency,
                Hospital = r.HospitalName ?? "",
                District = r.District != null ? r.District.Name : "",
                Thana = r.Thana != null ? r.Thana.Name : "",
                Created = r.CreatedDate.ToString("yyyy-MM-dd HH:mm")
            }).ToListAsync();

        return Ok(requests);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var bloodGroupStats = await _context.Donors
            .Where(d => d.Status)
            .GroupBy(d => d.BloodGroup)
            .Select(g => new { BloodGroup = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            bloodGroupStats,
            totalDonors = await _context.Donors.CountAsync(d => d.Status),
            pendingRequests = await _context.BloodRequests.CountAsync(r => r.Status == "Pending")
        });
    }
}
