using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationManagement.Models
{
    public class Donor
    {
        public int DonorId { get; set; }
        public string FullName { get; set; }
        public string BloodGroup { get; set; }

        public int? DistrictId { get; set; }

        public District District { get; set; }

        // ThanaId foreign key
        public int? ThanaId { get; set; }
        public Thana Thana { get; set; }

        public string ContactNo { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public bool Status { get; set; }
    }

}

