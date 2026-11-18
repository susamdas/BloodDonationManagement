using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodDonationManagement.Services;

/// <summary>A short "reason" shown on a donor card, e.g. "Exact match" or "2.4 km away".</summary>
public record MatchChip(string Text, string Icon, string Css);

public record ResponseHistory(int Notified, int Accepted);

public class DonorMatch
{
    public Donor Donor { get; init; } = default!;

    /// <summary>Final Match Score, 0-100.</summary>
    public double Score { get; set; }

    // Individual factors, each 0-1
    public double Compatibility { get; set; }
    public double Proximity { get; set; }
    public double Readiness { get; set; }
    public double Reliability { get; set; }

    public double? DistanceKm { get; set; }
    public bool IsExactMatch { get; set; }
    public EligibilityInfo Eligibility { get; set; } = default!;
    public int TimesNotified { get; set; }
    public int TimesAccepted { get; set; }

    /// <summary>Response of this donor to the current request, if any.</summary>
    public ResponseStatus? CurrentResponse { get; set; }

    public List<MatchChip> Chips { get; } = new();
}

public class MatchResult
{
    public List<DonorMatch> Matches { get; set; } = new();
    public double RadiusKm { get; set; }
    public bool RadiusExpanded { get; set; }

    // Transparency: why donors were filtered out
    public int Considered { get; set; }
    public int ExcludedIncompatible { get; set; }
    public int ExcludedOnCooldown { get; set; }
    public int ExcludedUnavailable { get; set; }
    public int ExcludedTooFar { get; set; }
}

/// <summary>
/// Smart Donor Matching &amp; Ranking (FR-1).
/// 1) Hard filters remove donors who cannot or should not be contacted.
/// 2) Remaining donors get a weighted Match Score from four factors:
///    Compatibility, Proximity, Readiness and Reliability.
/// </summary>
public class DonorMatchingService
{
    private static readonly HashSet<string> RareGroups = new(StringComparer.OrdinalIgnoreCase) { "O-", "A-", "B-", "AB-" };

    private readonly ApplicationDbContext _context;
    private readonly EligibilityService _eligibility;
    private readonly MatchingOptions _options;

    public DonorMatchingService(ApplicationDbContext context, EligibilityService eligibility, IOptions<MatchingOptions> options)
    {
        _context = context;
        _eligibility = eligibility;
        _options = options.Value;
    }

    public MatchingOptions Options => _options;

    public static bool IsRareGroup(string bloodGroup) => RareGroups.Contains(bloodGroup);

    public async Task<MatchResult> FindMatchesAsync(BloodRequest request)
    {
        var recipientGroup = request.BloodGroup.Trim().ToUpperInvariant();
        var compatibleGroups = BloodCompatibilityService.GetCompatibleDonorGroups(recipientGroup);

        var donors = await _context.Donors
            .AsNoTracking()
            .Include(d => d.District)
            .Include(d => d.Thana)
            .Where(d => d.Status)
            .ToListAsync();

        // Reliability history: how often each donor was notified and how often they said yes
        var history = await _context.DonorResponses
            .Where(r => r.BloodRequestId != request.BloodRequestId)
            .GroupBy(r => r.DonorId)
            .Select(g => new
            {
                DonorId = g.Key,
                Notified = g.Count(),
                Accepted = g.Count(r => r.Status == ResponseStatus.Accepted || r.Status == ResponseStatus.Confirmed)
            })
            .ToDictionaryAsync(x => x.DonorId, x => new ResponseHistory(x.Notified, x.Accepted));

        var currentResponses = request.BloodRequestId == 0
            ? new Dictionary<int, ResponseStatus>()
            : await _context.DonorResponses
                .Where(r => r.BloodRequestId == request.BloodRequestId)
                .ToDictionaryAsync(r => r.DonorId, r => r.Status);

        var (radius, expandedRadius) = GetRadius(request.Urgency);
        var result = new MatchResult { Considered = donors.Count, RadiusKm = radius };

        // Pass 1: hard filters that do not depend on distance
        var candidates = new List<(Donor Donor, EligibilityInfo Eligibility)>();
        foreach (var donor in donors)
        {
            if (!string.IsNullOrEmpty(request.RequesterUserId) && donor.UserId == request.RequesterUserId)
                continue; // never match the requester with themself

            if (!compatibleGroups.Contains(donor.BloodGroup, StringComparer.OrdinalIgnoreCase))
            {
                result.ExcludedIncompatible++;
                continue;
            }
            if (!donor.IsAvailable)
            {
                result.ExcludedUnavailable++;
                continue;
            }
            var eligibility = _eligibility.Evaluate(donor);
            if (!eligibility.IsEligible)
            {
                result.ExcludedOnCooldown++;
                continue;
            }
            candidates.Add((donor, eligibility));
        }

        // Pass 2: distance filter + scoring; widen the radius automatically if too few donors are found
        var matches = BuildMatches(request, candidates, radius, history, currentResponses, out var tooFar);
        if (matches.Count < _options.NotifyTopN && expandedRadius > radius)
        {
            matches = BuildMatches(request, candidates, expandedRadius, history, currentResponses, out tooFar);
            result.RadiusKm = expandedRadius;
            result.RadiusExpanded = true;
        }
        result.ExcludedTooFar = tooFar;

        result.Matches = matches
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.DistanceKm ?? double.MaxValue)
            .ToList();
        return result;
    }

    private List<DonorMatch> BuildMatches(
        BloodRequest request,
        List<(Donor Donor, EligibilityInfo Eligibility)> candidates,
        double radiusKm,
        IDictionary<int, ResponseHistory> history,
        IDictionary<int, ResponseStatus> currentResponses,
        out int excludedTooFar)
    {
        excludedTooFar = 0;
        var list = new List<DonorMatch>();
        var requestHasCoords = request.Latitude.HasValue && request.Longitude.HasValue;

        foreach (var (donor, eligibility) in candidates)
        {
            double? distance = null;
            double proximity;

            if (requestHasCoords && donor.Latitude.HasValue && donor.Longitude.HasValue)
            {
                distance = DistanceService.Haversine(request.Latitude!.Value, request.Longitude!.Value,
                                                     donor.Latitude.Value, donor.Longitude.Value);
                if (distance > radiusKm) { excludedTooFar++; continue; }
                proximity = ProximityScore(distance.Value, radiusKm);
            }
            else if (request.DistrictId.HasValue)
            {
                // No coordinates: fall back to administrative area
                if (donor.DistrictId != request.DistrictId) { excludedTooFar++; continue; }
                proximity = request.ThanaId.HasValue && donor.ThanaId == request.ThanaId ? 0.8 : 0.5;
            }
            else
            {
                proximity = 0.5; // request has no location at all: neutral
            }

            int notified = 0, accepted = 0;
            if (history.TryGetValue(donor.DonorId, out var h)) { notified = h.Notified; accepted = h.Accepted; }

            var match = new DonorMatch
            {
                Donor = donor,
                Eligibility = eligibility,
                DistanceKm = distance,
                IsExactMatch = string.Equals(donor.BloodGroup, request.BloodGroup, StringComparison.OrdinalIgnoreCase),
                Compatibility = CompatibilityScore(donor.BloodGroup, request.BloodGroup),
                Proximity = proximity,
                Readiness = ReadinessScore(eligibility.DaysSinceLastDonation, eligibility.CooldownDays, _options.ReadinessFullAfterDays),
                Reliability = ReliabilityScore(notified, accepted, _options.ReliabilityMinSamples),
                TimesNotified = notified,
                TimesAccepted = accepted,
                CurrentResponse = currentResponses.TryGetValue(donor.DonorId, out var s) ? s : null
            };
            match.Score = ComputeScore(_options, match.Compatibility, match.Proximity, match.Readiness, match.Reliability);
            AddChips(match, request);
            list.Add(match);
        }
        return list;
    }

    private (double Initial, double Expanded) GetRadius(UrgencyLevel urgency) => urgency switch
    {
        UrgencyLevel.Critical => (_options.RadiusCriticalKm, _options.RadiusCriticalExpandedKm),
        UrgencyLevel.Urgent => (_options.RadiusUrgentKm, _options.RadiusUrgentExpandedKm),
        _ => (_options.RadiusNormalKm, _options.RadiusNormalExpandedKm)
    };

    private static void AddChips(DonorMatch m, BloodRequest request)
    {
        // Compatibility
        if (m.IsExactMatch)
            m.Chips.Add(new("Exact match", "fa-check-double", "chip-good"));
        else
            m.Chips.Add(new($"Compatible ({m.Donor.BloodGroup})", "fa-check", "chip-ok"));

        // Proximity
        if (m.DistanceKm.HasValue)
            m.Chips.Add(new($"{m.DistanceKm.Value:0.0} km away", "fa-location-dot", m.DistanceKm < 5 ? "chip-good" : "chip-ok"));
        else if (request.ThanaId.HasValue && m.Donor.ThanaId == request.ThanaId)
            m.Chips.Add(new("Same thana", "fa-location-dot", "chip-good"));
        else if (request.DistrictId.HasValue)
            m.Chips.Add(new("Same district", "fa-location-dot", "chip-ok"));

        // Readiness
        if (m.Eligibility.DaysSinceLastDonation is int days)
            m.Chips.Add(new($"Eligible ({days} days since last)", "fa-heart-pulse", "chip-good"));
        else
            m.Chips.Add(new("Eligible (no recent donation)", "fa-heart-pulse", "chip-good"));

        // Reliability
        if (m.TimesNotified == 0)
            m.Chips.Add(new("New donor", "fa-seedling", "chip-neutral"));
        else
            m.Chips.Add(new($"{m.Reliability * 100:0}% response rate", "fa-handshake",
                m.Reliability >= 0.6 ? "chip-good" : m.Reliability >= 0.3 ? "chip-ok" : "chip-warn"));

        if (IsRareGroup(m.Donor.BloodGroup))
            m.Chips.Add(new("Rare group", "fa-gem", "chip-rare"));
    }

    // ---------------------------------------------------------------------
    // Pure scoring functions (public + static so they can be unit tested)
    // ---------------------------------------------------------------------

    /// <summary>Exact = 1.0, compatible = 0.7, O- giving to a non-O- patient = 0.5 (preserve the universal donor).</summary>
    public static double CompatibilityScore(string donorGroup, string recipientGroup)
    {
        if (string.Equals(donorGroup, recipientGroup, StringComparison.OrdinalIgnoreCase)) return 1.0;
        if (!BloodCompatibilityService.CanDonateToRecipient(donorGroup, recipientGroup)) return 0.0;
        if (string.Equals(donorGroup, "O-", StringComparison.OrdinalIgnoreCase)) return 0.5;
        return 0.7;
    }

    /// <summary>Linear decay: 1.0 at the hospital, 0.0 at the edge of the search radius.</summary>
    public static double ProximityScore(double distanceKm, double radiusKm) =>
        radiusKm <= 0 ? 0 : Math.Clamp(1 - distanceKm / radiusKm, 0, 1);

    /// <summary>Never donated = 1.0; otherwise grows from 0 (just eligible) to 1 after <paramref name="fullAfterDays"/>.</summary>
    public static double ReadinessScore(int? daysSinceLastDonation, int cooldownDays, int fullAfterDays)
    {
        if (!daysSinceLastDonation.HasValue) return 1.0;
        if (fullAfterDays <= 0) return 1.0;
        return Math.Clamp((daysSinceLastDonation.Value - cooldownDays) / (double)fullAfterDays, 0, 1);
    }

    /// <summary>accepted / notified; new donors (few notifications) get a neutral 0.5.</summary>
    public static double ReliabilityScore(int notified, int accepted, int minSamples)
    {
        if (notified < Math.Max(1, minSamples)) return 0.5;
        return Math.Clamp(accepted / (double)notified, 0, 1);
    }

    /// <summary>Weighted sum of the four factors, returned as 0-100.</summary>
    public static double ComputeScore(MatchingOptions o, double compatibility, double proximity, double readiness, double reliability)
    {
        var totalWeight = o.WeightCompatibility + o.WeightProximity + o.WeightReadiness + o.WeightReliability;
        if (totalWeight <= 0) return 0;
        var raw = o.WeightCompatibility * compatibility
                + o.WeightProximity * proximity
                + o.WeightReadiness * readiness
                + o.WeightReliability * reliability;
        return Math.Round(raw / totalWeight * 100, 1);
    }
}
