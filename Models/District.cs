using BloodDonationManagement.Models;
using System.ComponentModel.DataAnnotations;

public class District
{
    [Key]
    public int DistrictId { get; set; }
    public int DivisionId { get; set; }
    public string District_Code { get; set; }
    public string ZoneCode { get; set; }
    public string GbDistrictCode { get; set; }
    public string District_Name_Bng { get; set; }
    public string District_Name_Eng { get; set; }
    public string Division_Code { get; set; }
    public string Division_Name_Bng { get; set; }
    public string Division_Name_Eng { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? InActiveDate { get; set; }
    public long? CreateUser { get; set; }
    public DateTime? CreateDate { get; set; }
    public long? UpdateUser { get; set; }
    public DateTime? UpdateDate { get; set; }

    // 🔗 Navigation
    public ICollection<Thana> Thanas { get; set; }
}
