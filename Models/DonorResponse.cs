using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

public enum ResponseStatus
{
    Notified = 0,
    Accepted = 1,
    Declined = 2,
    /// <summary>Requester confirmed that this donor actually donated.</summary>
    Confirmed = 3
}

/// <summary>Links a donor to a blood request: notification, answer and outcome.</summary>
public class DonorResponse
{
    public int DonorResponseId { get; set; }

    public int BloodRequestId { get; set; }
    public BloodRequest? BloodRequest { get; set; }

    public int DonorId { get; set; }
    public Donor? Donor { get; set; }

    public ResponseStatus Status { get; set; } = ResponseStatus.Notified;

    /// <summary>Match score (0-100) at the time the donor was notified.</summary>
    public double MatchScore { get; set; }

    public double? DistanceKm { get; set; }

    public DateTime NotifiedAt { get; set; } = DateTime.Now;
    public DateTime? RespondedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    [StringLength(250)]
    public string? Note { get; set; }
}
