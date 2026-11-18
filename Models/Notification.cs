using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

/// <summary>Persistent in-app notification for a user (shown in the bell).</summary>
public class Notification
{
    public int NotificationId { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = default!;

    [Required, StringLength(150)]
    public string Title { get; set; } = default!;

    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Link { get; set; }

    /// <summary>info | success | warning | danger</summary>
    [StringLength(20)]
    public string Type { get; set; } = "info";

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
