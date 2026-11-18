using BloodDonationManagement.Models;
using Microsoft.Extensions.Options;

namespace BloodDonationManagement.Services;

public record EligibilityInfo(
    bool IsEligible,
    int CooldownDays,
    int? DaysSinceLastDonation,
    DateTime? NextEligibleDate,
    int DaysRemaining);

/// <summary>Decides whether a donor may donate today (90 days male / 120 days female by default).</summary>
public class EligibilityService
{
    private readonly MatchingOptions _options;

    public EligibilityService(IOptions<MatchingOptions> options) => _options = options.Value;

    public int GetCooldownDays(Gender gender) =>
        gender == Gender.Female ? _options.CooldownDaysFemale : _options.CooldownDaysMale;

    public EligibilityInfo Evaluate(Donor donor) => Evaluate(donor.Gender, donor.LastDonationDate, DateTime.Today);

    public EligibilityInfo Evaluate(Gender gender, DateTime? lastDonation, DateTime today)
    {
        var cooldown = GetCooldownDays(gender);
        if (!lastDonation.HasValue)
            return new EligibilityInfo(true, cooldown, null, null, 0);

        var daysSince = (int)(today.Date - lastDonation.Value.Date).TotalDays;
        var next = lastDonation.Value.Date.AddDays(cooldown);
        var remaining = Math.Max(0, (int)(next - today.Date).TotalDays);
        return new EligibilityInfo(remaining == 0, cooldown, daysSince, next, remaining);
    }
}
