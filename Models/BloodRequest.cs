using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

public enum UrgencyLevel
{
    Normal = 0,
    Urgent = 1,
    Critical = 2
}

public enum RequestStatus
{
    Pending = 0,
    Fulfilled = 1,
    [Display(Name = "Donor Matched")]
    DonorMatched = 2,
    Cancelled = 3,
    Expired = 4
}

public class BloodRequest : IValidatableObject
{
    public int BloodRequestId { get; set; }

    /// <summary>Identity user who created the request.</summary>
    [StringLength(450)]
    public string? RequesterUserId { get; set; }

    [Required]
    [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Please enter a valid blood group like A+, B-, O+, or AB-.")]
    [Display(Name = "Blood Group")]
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

    [Range(1, 10, ErrorMessage = "Units needed must be between 1 and 10.")]
    [Display(Name = "Units (bags) Needed")]
    public int UnitsNeeded { get; set; } = 1;

    [Display(Name = "Needed By")]
    public DateTime? NeededBy { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    [Display(Name = "Urgency Level")]
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Normal;

    /// <summary>Set by an admin after checking the request is genuine.</summary>
    [Display(Name = "Verified")]
    public bool IsVerified { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? FulfilledAt { get; set; }

    public ICollection<DonorResponse> Responses { get; set; } = new List<DonorResponse>();

    public bool IsOpen => Status is RequestStatus.Pending or RequestStatus.DonorMatched;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BloodRequestId == 0 && NeededBy.HasValue && NeededBy.Value < DateTime.Now.AddMinutes(-5))
        {
            yield return new ValidationResult("'Needed by' cannot be in the past.", new[] { nameof(NeededBy) });
        }
    }
}
