using System.Collections.Generic;
using BloodDonationManagement.Models;
using BloodDonationManagement.Services;

namespace BloodDonationManagement.ViewModels;

/// <summary>Data for the public home page.</summary>
public class DashboardViewModel
{
    // Live panel + impact
    public int ActiveDonors { get; set; }
    public int EligibleNow { get; set; }
    public int OpenRequests { get; set; }
    public int CriticalOpen { get; set; }
    public int FulfilledRequests { get; set; }
    public int TotalDonations { get; set; }
    public double? AvgMinutesToFirstAccept { get; set; }
    public int DistrictsCovered { get; set; }

    /// <summary>Eligible-today donors per blood group (all 8 groups present).</summary>
    public Dictionary<string, int> EligibleByGroup { get; set; } = new();

    /// <summary>Open requests, most urgent first.</summary>
    public List<BloodRequest> UrgentRequests { get; set; } = new();

    // "For you" strip
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
    public Donor? MyDonor { get; set; }
    public EligibilityInfo? MyEligibility { get; set; }
    public int MyPendingAlerts { get; set; }
    public int MyOpenRequests { get; set; }
    public int OpenReports { get; set; }
}
