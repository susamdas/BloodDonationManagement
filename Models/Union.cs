using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models
{
    public class Union
    {
        [Key]
        public int UnionId { get; set; }
        public int? ThanaId { get; set; }
        public string Union_Code { get; set; }
        public string Union_Name_Eng { get; set; }
        public string? Union_Name_Bng { get; set; }
        // 🔗 Navigation
        public Thana Thana { get; set; }
    }
}
