using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

public enum Gender
{
    Male = 0,
    Female = 1
}

public class Donor : IValidatableObject
{
    public int DonorId { get; set; }

    /// <summary>Identity user that owns this donor profile (null for donors added by an admin).</summary>
    [StringLength(450)]
    public string? UserId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = default!;

    [Required]
    [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Please enter a valid blood group like A+, B-, O+, or AB-.")]
    [Display(Name = "Blood Group")]
    public string BloodGroup { get; set; } = default!;

    [Required]
    public Gender Gender { get; set; } = Gender.Male;

    public int? DistrictId { get; set; }
    public District? District { get; set; }

    public int? ThanaId { get; set; }
    public Thana? Thana { get; set; }

    [Required]
    [Phone]
    [Display(Name = "Contact Number")]
    public string ContactNo { get; set; } = default!;

    [DataType(DataType.Date)]
    [Display(Name = "Last Donation Date")]
    public DateTime? LastDonationDate { get; set; }

    [Display(Name = "Latitude")]
    public double? Latitude { get; set; }

    [Display(Name = "Longitude")]
    public double? Longitude { get; set; }

    /// <summary>Account active (admin controlled).</summary>
    [Display(Name = "Active")]
    public bool Status { get; set; } = true;

    /// <summary>Donor's own switch: receive alerts and appear in matching.</summary>
    [Display(Name = "Available to donate now")]
    public bool IsAvailable { get; set; } = true;

    /// <summary>Hide profile from public search and map.</summary>
    [Display(Name = "Hide my profile from public search")]
    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    public ICollection<DonorResponse> Responses { get; set; } = new List<DonorResponse>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // A recent date is allowed: it simply makes the donor ineligible for a while.
        if (LastDonationDate.HasValue && LastDonationDate.Value.Date > DateTime.Today)
        {
            yield return new ValidationResult(
                "Last donation date cannot be in the future.",
                new[] { nameof(LastDonationDate) });
        }
    }
}
