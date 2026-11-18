namespace BloodDonationManagement.Services;

/// <summary>Helpers that keep donor personal data private (FR-5).</summary>
public static class PrivacyHelper
{
    /// <summary>01712345678 -> 017•••••678</summary>
    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var digits = phone.Trim();
        if (digits.Length <= 6) return new string('•', digits.Length);
        return digits[..3] + new string('•', digits.Length - 6) + digits[^3..];
    }

    /// <summary>Round coordinates to about 1 km so a donor's exact home is never exposed.</summary>
    public static double? Approximate(double? coordinate) =>
        coordinate.HasValue ? Math.Round(coordinate.Value, 2) : null;
}
