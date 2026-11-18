using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

/// <summary>A completed blood donation (history record).</summary>
public class Donation
{
    public int DonationId { get; set; }

    public int DonorId { get; set; }
    public Donor? Donor { get; set; }

    public int? BloodRequestId { get; set; }
    public BloodRequest? BloodRequest { get; set; }

    [DataType(DataType.Date)]
    public DateTime DonationDate { get; set; } = DateTime.Today;

    public int Units { get; set; } = 1;

    [StringLength(200)]
    public string? HospitalName { get; set; }
}
