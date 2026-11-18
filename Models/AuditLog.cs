using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

/// <summary>
/// Append-only record of who did what and when. Written by AuditService; never edited or deleted from the UI.
/// </summary>
public class AuditLog
{
    public long AuditLogId { get; set; }

    public DateTime At { get; set; } = DateTime.Now;

    [StringLength(450)]
    public string? ActorUserId { get; set; }

    [StringLength(256)]
    public string ActorEmail { get; set; } = "system";

    public bool ActorIsAdmin { get; set; }

    /// <summary>Short machine-friendly action, e.g. "User.Deactivated", "Request.ContactShared".</summary>
    [Required, StringLength(60)]
    public string Action { get; set; } = default!;

    /// <summary>Request | Donor | User | Report | District | Thana</summary>
    [StringLength(30)]
    public string? EntityType { get; set; }

    [StringLength(60)]
    public string? EntityId { get; set; }

    /// <summary>Human-readable description shown in the audit page.</summary>
    [StringLength(500)]
    public string Details { get; set; } = string.Empty;

    [StringLength(64)]
    public string? IpAddress { get; set; }
}
