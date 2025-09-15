using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models
{
    public class Thana
    {
        [Key]
        public int ThanaId { get; set; }
        public int? DistrictId { get; set; }
        public string? Thana_Code { get; set; }
        public string? Thana_Name_Eng { get; set; }
        public string? Thana_Name_Bng { get; set; }

        // 🔗 Navigation
        public District District { get; set; }
        public ICollection<Union> Unions { get; set; } = new List<Union>();
    }
}
