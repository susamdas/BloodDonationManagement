using BloodDonationManagement.Models;
using BloodDonationManagement.Services;

namespace BloodDonationManagement.ViewModels;

public record TimelineItem(DateTime At, string Title, string Detail, string Icon, string Css);

public class RequestDetailsViewModel
{
    public BloodRequest Request { get; set; } = default!;

    /// <summary>Requester (owner) or admin: can see ranking, notify donors and confirm donations.</summary>
    public bool CanManage { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsAuthenticated { get; set; }

    /// <summary>Smart Matching result (only for managers while the request is open).</summary>
    public MatchResult? Match { get; set; }

    // Current user as a donor
    public Donor? MyDonor { get; set; }
    public DonorResponse? MyResponse { get; set; }
    public EligibilityInfo? MyEligibility { get; set; }
    public bool MyGroupCompatible { get; set; }

    public List<DonorResponse> Responses { get; set; } = new();
    public int ConfirmedUnits { get; set; }
    public List<TimelineItem> Timeline { get; set; } = new();

    public bool IsOwner { get; set; }
    public bool AlreadyReported { get; set; }
    public int OpenReports { get; set; }

    public bool CanRespond =>
        Request.IsOpen && MyDonor != null && MyGroupCompatible && (MyEligibility?.IsEligible ?? false)
        && MyResponse?.Status is not ResponseStatus.Confirmed;
}
