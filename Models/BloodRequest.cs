using BloodDonationManagement.Models;
using System.ComponentModel.DataAnnotations;

public class BloodRequest
{
    public int BloodRequestId { get; set; } // Primary Key

    [Required]
    public string BloodGroup { get; set; }

    public int? DistrictId { get; set; }
    public District District { get; set; }

    public int? ThanaId { get; set; }
    public Thana Thana { get; set; }

    [Required, Phone]
    public string ContactNo { get; set; }

    public string Message { get; set; }

    [Required]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
