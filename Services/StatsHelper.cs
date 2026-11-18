using BloodDonationManagement.Models;

namespace BloodDonationManagement.Services;

public static class StatsHelper
{
    /// <summary>
    /// Average minutes from a request being posted to the first donor accepting it.
    /// Requests must be loaded with their Responses. Returns null when there is no data.
    /// </summary>
    public static double? AvgMinutesToFirstAccept(IEnumerable<BloodRequest> requests)
    {
        var minutes = requests
            .Select(r => new
            {
                r.CreatedDate,
                First = r.Responses
                    .Where(x => x.RespondedAt.HasValue && (x.Status == ResponseStatus.Accepted || x.Status == ResponseStatus.Confirmed))
                    .Select(x => x.RespondedAt!.Value)
                    .DefaultIfEmpty()
                    .Min()
            })
            .Where(x => x.First != default && x.First >= x.CreatedDate)
            .Select(x => (x.First - x.CreatedDate).TotalMinutes)
            .ToList();
        return minutes.Count == 0 ? null : Math.Round(minutes.Average(), 1);
    }

    /// <summary>"24 min", "3.5 h", "2.0 days"</summary>
    public static string FormatDuration(double? minutes) => minutes switch
    {
        null => "—",
        < 60 => $"{minutes:0} min",
        < 1440 => $"{minutes / 60:0.0} h",
        _ => $"{minutes / 1440:0.0} days"
    };

    /// <summary>"just now", "12 min ago", "3 h ago", "2 days ago"</summary>
    public static string TimeAgo(DateTime when)
    {
        var span = DateTime.Now - when;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
        return $"{(int)span.TotalDays} day{((int)span.TotalDays == 1 ? "" : "s")} ago";
    }
}
