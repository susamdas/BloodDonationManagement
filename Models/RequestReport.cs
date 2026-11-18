using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

public enum ReportStatus
{
    Open = 0,
    Dismissed = 1,
    ActionTaken = 2
}

/// <summary>A user's report that a blood request looks fake, asks for money, etc.</summary>
public class RequestReport
{
    public int RequestReportId { get; set; }

    public int BloodRequestId { get; set; }
    public BloodRequest? BloodRequest { get; set; }

    [Required, StringLength(450)]
    public string ReporterUserId { get; set; } = default!;

    [Required, StringLength(50)]
    public string Reason { get; set; } = default!;

    [StringLength(500)]
    public string? Details { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
