using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

public class Donor : IValidatableObject
{
    public int DonorId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = default!;

    [Required]
    [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Please enter a valid blood group like A+, B-, O+, or AB-.")]
    public string BloodGroup { get; set; } = default!;

    public int? DistrictId { get; set; }
    public District? District { get; set; }

    public int? ThanaId { get; set; }
    public Thana? Thana { get; set; }

    [Required]
    [Phone]
    [Display(Name = "Contact Number")]
    public string ContactNo { get; set; } = default!;

    [DataType(DataType.Date)]
    public DateTime? LastDonationDate { get; set; }

    [Display(Name = "Latitude")]
    public double? Latitude { get; set; }

    [Display(Name = "Longitude")]
    public double? Longitude { get; set; }

    public bool Status { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LastDonationDate.HasValue)
        {
            var minimumAllowedDate = DateTime.Today.AddDays(-90);
            if (LastDonationDate.Value > minimumAllowedDate)
            {
                yield return new ValidationResult(
                    "Minimum 90 days gap required between donations.",
                    new[] { nameof(LastDonationDate) });
            }
        }
    }
}

