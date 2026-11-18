using BloodDonationManagement.Models;
using BloodDonationManagement.Services;

namespace BloodDonationManagement.ViewModels;

public class MyDonorProfileViewModel
{
    public Donor Donor { get; set; } = default!;
    public EligibilityInfo Eligibility { get; set; } = default!;
    public List<Donation> Donations { get; set; } = new();
    public List<DonorResponse> Responses { get; set; } = new();

    /// <summary>Requests this donor was alerted about and has not answered yet.</summary>
    public List<DonorResponse> PendingRequests { get; set; } = new();

    public int TimesNotified { get; set; }
    public int TimesAccepted { get; set; }

    public int ResponseRate => TimesNotified == 0 ? 0 : (int)Math.Round(TimesAccepted * 100.0 / TimesNotified);
    public int TotalDonations => Donations.Count;
}
