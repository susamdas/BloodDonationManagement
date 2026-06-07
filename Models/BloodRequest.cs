using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

public enum UrgencyLevel
{
    Normal = 0,
    Urgent = 1,
    Critical = 2
}

public class BloodRequest
{
    public int BloodRequestId { get; set; }

    [Required]
    [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Please enter a valid blood group like A+, B-, O+, or AB-.")]
    public string BloodGroup { get; set; } = default!;

    public int? DistrictId { get; set; }
    public District? District { get; set; }

    public int? ThanaId { get; set; }
    public Thana? Thana { get; set; }

    [Required]
    [Display(Name = "Patient Name")]
    public string PatientName { get; set; } = default!;

    [Required]
    [Phone]
    [Display(Name = "Contact Number")]
    public string ContactNo { get; set; } = default!;

    [Display(Name = "Hospital / Clinic")]
    public string? HospitalName { get; set; }

    [Display(Name = "Latitude")]
    public double? Latitude { get; set; }

    [Display(Name = "Longitude")]
    public double? Longitude { get; set; }

    public string Message { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Urgency Level")]
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Normal;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
