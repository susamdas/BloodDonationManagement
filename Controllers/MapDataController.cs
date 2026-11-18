using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using BloodDonationManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MapDataController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    private readonly EligibilityService _eligibility;
    private readonly DonorMatchingService _matching;

    public MapDataController(ApplicationDbContext context, EligibilityService eligibility, DonorMatchingService matching)
    {
        _context = context;
        _eligibility = eligibility;
        _matching = matching;
    }

    /// <summary>
    /// Live preview for the request form: how many donors Smart Matching would find.
    /// Returns counts and scores only (no names or phone numbers).
    /// </summary>
    [HttpGet("match-preview")]
    public async Task<IActionResult> MatchPreview(string bloodGroup, UrgencyLevel urgency = UrgencyLevel.Normal,
        int? districtId = null, int? thanaId = null, double? lat = null, double? lng = null)
    {
        if (string.IsNullOrWhiteSpace(bloodGroup) || !BloodCompatibilityService.IsValidGroup(bloodGroup))
            return BadRequest();

        var probe = new BloodRequest
        {
            BloodGroup = bloodGroup.Trim().ToUpperInvariant(),
            Urgency = urgency,
            DistrictId = districtId,
            ThanaId = thanaId,
            Latitude = lat,
            Longitude = lng,
            RequesterUserId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null,
            PatientName = "preview",
            ContactNo = "0"
        };
        var result = await _matching.FindMatchesAsync(probe);
        return Ok(new
        {
            matched = result.Matches.Count,
            radiusKm = result.RadiusKm,
            expanded = result.RadiusExpanded,
            hasLocation = lat.HasValue && lng.HasValue,
            bestScore = result.Matches.Count > 0 ? Math.Round(result.Matches[0].Score) : 0,
            notifyTopN = _matching.Options.NotifyTopN,
            excluded = new
            {
                incompatible = result.ExcludedIncompatible,
                resting = result.ExcludedOnCooldown,
                paused = result.ExcludedUnavailable,
                tooFar = result.ExcludedTooFar
            }
        });
    }

    private static string FirstNameOnly(string name) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? name;

    [HttpGet("donors")]
    public async Task<IActionResult> GetDonors([FromQuery] string? bloodGroup)
    {
        var query = _context.Donors
            .Include(d => d.District)
            .Include(d => d.Thana)
            .Where(d => d.Status && !d.IsHidden && d.Latitude.HasValue && d.Longitude.HasValue);

        if (!string.IsNullOrWhiteSpace(bloodGroup))
            query = query.Where(d => d.BloodGroup == bloodGroup.ToUpper());

        var list = await query.ToListAsync();
        var isAdmin = User.IsInRole("Admin");

        // Privacy: masked phone and approximate (~1 km) position for the public map
        var donors = list.Select(d =>
        {
            var e = _eligibility.Evaluate(d);
            return new
            {
                d.DonorId,
                FullName = isAdmin ? d.FullName : FirstNameOnly(d.FullName),
                d.BloodGroup,
                ContactNo = isAdmin ? d.ContactNo : PrivacyHelper.MaskPhone(d.ContactNo),
                Latitude = isAdmin ? d.Latitude : PrivacyHelper.Approximate(d.Latitude),
                Longitude = isAdmin ? d.Longitude : PrivacyHelper.Approximate(d.Longitude),
                District = d.District != null ? d.District.Name : "",
                Thana = d.Thana != null ? d.Thana.Name : "",
                LastDonation = d.LastDonationDate != null ? d.LastDonationDate.Value.ToString("yyyy-MM-dd") : "",
                Eligible = e.IsEligible && d.IsAvailable
            };
        });

        return Ok(donors);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        var requests = await _context.BloodRequests
            .Include(r => r.District)
            .Include(r => r.Thana)
            .Where(r => (r.Status == RequestStatus.Pending || r.Status == RequestStatus.DonorMatched) && r.Latitude.HasValue && r.Longitude.HasValue)
            .Select(r => new
            {
                r.BloodRequestId,
                r.BloodGroup,
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
            pendingRequests = await _context.BloodRequests.CountAsync(r => r.Status == RequestStatus.Pending)
        });
    }
}
