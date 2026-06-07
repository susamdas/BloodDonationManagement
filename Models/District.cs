using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

public class District
{
    public int DistrictId { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    public ICollection<Thana> Thanas { get; set; } = new List<Thana>();
    public ICollection<Donor> Donors { get; set; } = new List<Donor>();
}
