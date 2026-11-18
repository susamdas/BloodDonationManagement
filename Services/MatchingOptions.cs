namespace BloodDonationManagement.Services;

/// <summary>
/// Tunable settings for eligibility and Smart Donor Matching.
/// Bound from the "Matching" section of appsettings.json, so weights can change without code changes.
/// </summary>
public class MatchingOptions
{
    public const string SectionName = "Matching";

    // Eligibility (days between donations)
    public int CooldownDaysMale { get; set; } = 90;
    public int CooldownDaysFemale { get; set; } = 120;

    // Score weights (should add up to 1.0)
    public double WeightCompatibility { get; set; } = 0.35;
    public double WeightProximity { get; set; } = 0.30;
    public double WeightReadiness { get; set; } = 0.20;
    public double WeightReliability { get; set; } = 0.15;

    // Search radius in km by urgency: initial, then expanded if too few donors are found
    public double RadiusNormalKm { get; set; } = 10;
    public double RadiusNormalExpandedKm { get; set; } = 25;
    public double RadiusUrgentKm { get; set; } = 15;
    public double RadiusUrgentExpandedKm { get; set; } = 40;
    public double RadiusCriticalKm { get; set; } = 25;
    public double RadiusCriticalExpandedKm { get; set; } = 60;

    /// <summary>Number of donors automatically notified when a request is created.</summary>
    public int NotifyTopN { get; set; } = 5;

    /// <summary>Days after the cooldown at which readiness reaches 100%.</summary>
    public int ReadinessFullAfterDays { get; set; } = 180;

    /// <summary>Notifications needed before the reliability score is trusted.</summary>
    public int ReliabilityMinSamples { get; set; } = 3;
}
