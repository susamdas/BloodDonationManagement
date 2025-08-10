using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models
{
    public class District
    {
        public int DistrictId { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<Thana> Thanas { get; set; }
        public virtual ICollection<Donor> Donors { get; set; }
    }
}
