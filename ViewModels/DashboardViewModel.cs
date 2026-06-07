using System.Collections.Generic;
using BloodDonationManagement.Models;

namespace BloodDonationManagement.ViewModels;

public class DashboardViewModel
{
    public int TotalDonors { get; set; }
    public int ActiveDonors { get; set; }
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int FulfilledRequests { get; set; }
    public int DistrictCount { get; set; }
    public int ThanaCount { get; set; }
    public Dictionary<string, int> BloodGroupStats { get; set; } = new();
    public List<BloodRequest> RecentRequests { get; set; } = new();
}
