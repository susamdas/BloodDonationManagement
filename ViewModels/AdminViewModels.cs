using BloodDonationManagement.Models;

namespace BloodDonationManagement.ViewModels;

public record LabelValue(string Label, int Value);

public record ReliableDonor(int DonorId, string Name, string BloodGroup, int Donations, int ResponseRate);

public class AnalyticsViewModel
{
    // KPI tiles
    public int ActiveDonors { get; set; }
    public int EligibleNow { get; set; }
    public int OpenRequests { get; set; }
    public int CriticalOpen { get; set; }
    public int FulfilledRequests { get; set; }
    public int FulfilmentRate { get; set; }
    public double? AvgMinutesToFirstAccept { get; set; }
    public int AcceptanceRate { get; set; }
    public int TotalDonations { get; set; }
    public int RareEligible { get; set; }
    public int OpenReports { get; set; }

    // Charts
    public List<string> BloodGroups { get; set; } = new();
    public List<int> EligibleByGroup { get; set; } = new();
    public List<int> RequestsByGroup { get; set; } = new();

    public List<string> TrendDays { get; set; } = new();
    public List<int> TrendCreated { get; set; } = new();
    public List<int> TrendFulfilled { get; set; } = new();

    public List<LabelValue> ByUrgency { get; set; } = new();
    public List<LabelValue> TopDistricts { get; set; } = new();
    public List<ReliableDonor> TopDonors { get; set; } = new();
}

public class UserRow
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public Donor? Donor { get; set; }
    public int Requests { get; set; }
}

public class UsersViewModel
{
    public List<UserRow> Users { get; set; } = new();
    public string? Search { get; set; }
    public string? CurrentUserId { get; set; }
}
