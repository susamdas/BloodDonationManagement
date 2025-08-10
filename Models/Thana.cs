using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models
{
    public class Thana
    {
        public int ThanaId { get; set; }

        [Required]
        public string Name { get; set; }

        public int DistrictId { get; set; }
        public District District { get; set; }
        public virtual ICollection<Donor> Donors { get; set; }
    }
}
