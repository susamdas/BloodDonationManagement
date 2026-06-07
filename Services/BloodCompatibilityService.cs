namespace BloodDonationManagement.Services;

public static class BloodCompatibilityService
{
    private static readonly Dictionary<string, string[]> _compatibleDonors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A+"]  = ["A+", "A-", "O+", "O-"],
        ["A-"]  = ["A-", "O-"],
        ["B+"]  = ["B+", "B-", "O+", "O-"],
        ["B-"]  = ["B-", "O-"],
        ["AB+"] = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"],
        ["AB-"] = ["A-", "B-", "AB-", "O-"],
        ["O+"]  = ["O+", "O-"],
        ["O-"]  = ["O-"],
    };

    public static string[] GetCompatibleDonorGroups(string recipientGroup) =>
        _compatibleDonors.TryGetValue(recipientGroup, out var groups) ? groups : [recipientGroup];

    public static bool CanDonateToRecipient(string donorGroup, string recipientGroup) =>
        GetCompatibleDonorGroups(recipientGroup).Contains(donorGroup, StringComparer.OrdinalIgnoreCase);
}
