using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models;

public class Thana
{
    public int ThanaId { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    public int DistrictId { get; set; }
    public District? District { get; set; }
    public ICollection<Donor> Donors { get; set; } = new List<Donor>();
}
