using System.Security.Claims;
using BloodDonationManagement.Data;
using BloodDonationManagement.Models;

namespace BloodDonationManagement.Services;

/// <summary>
/// Writes the audit trail (who did what, when, from where).
/// Used for every admin action and for privacy-sensitive events such as sharing contact details.
/// </summary>
public class AuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor http, ILogger<AuditService> logger)
    {
        _context = context;
        _http = http;
        _logger = logger;
    }

    public async Task LogAsync(string action, string? entityType, object? entityId, string details)
    {
        var ctx = _http.HttpContext;
        var user = ctx?.User;
        var entry = new AuditLog
        {
            At = DateTime.Now,
            ActorUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier),
            ActorEmail = user?.Identity?.IsAuthenticated == true ? (user.Identity!.Name ?? "unknown") : "anonymous",
            ActorIsAdmin = user?.IsInRole("Admin") == true,
            Action = action,
            EntityType = entityType,
            EntityId = entityId?.ToString(),
            Details = details.Length > 500 ? details[..500] : details,
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString()
        };

        try
        {
            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Auditing must never break the user's action, but failures are logged loudly.
            _logger.LogError(ex, "Failed to write audit entry {Action} for {Entity} {Id}", action, entityType, entityId);
        }
    }
}

/// <summary>Action names used in the audit trail (kept in one place for filtering).</summary>
public static class AuditActions
{
    // Admin: users
    public const string UserDeactivated = "User.Deactivated";
    public const string UserActivated = "User.Activated";
    public const string AdminGranted = "User.AdminGranted";
    public const string AdminRevoked = "User.AdminRevoked";

    // Admin: requests & moderation
    public const string RequestVerified = "Request.Verified";
    public const string RequestUnverified = "Request.Unverified";
    public const string RequestEdited = "Request.Edited";
    public const string RequestDeleted = "Request.Deleted";
    public const string RequestFulfilledByAdmin = "Request.FulfilledByAdmin";
    public const string ReportRemoved = "Report.RequestRemoved";
    public const string ReportVerified = "Report.Verified";
    public const string ReportDismissed = "Report.Dismissed";

    // Admin: donors & master data
    public const string DonorEditedByAdmin = "Donor.EditedByAdmin";
    public const string DonorDeleted = "Donor.Deleted";
    public const string LocationChanged = "Location.Changed";

    // Community events (privacy & lifecycle)
    public const string RequestCreated = "Request.Created";
    public const string RequestCancelled = "Request.Cancelled";
    public const string ContactShared = "Request.ContactShared";
    public const string DonationConfirmed = "Request.DonationConfirmed";
    public const string RequestReported = "Request.Reported";
}
