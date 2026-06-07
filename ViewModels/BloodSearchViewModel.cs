using System.Collections.Generic;
using BloodDonationManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BloodDonationManagement.ViewModels;

public class DonorWithDistance
{
    public Donor Donor { get; set; } = default!;
    public double? DistanceKm { get; set; }
}

public class BloodSearchViewModel
{
    public string? BloodGroup { get; set; }
    public int? DistrictId { get; set; }
    public int? ThanaId { get; set; }
    public bool CompatibilitySearch { get; set; }
    public double? UserLat { get; set; }
    public double? UserLng { get; set; }
    public double RadiusKm { get; set; } = 20;

    public List<SelectListItem> BloodGroups { get; set; } = new();
    public List<SelectListItem> Districts { get; set; } = new();
    public List<SelectListItem> Thanas { get; set; } = new();
    public List<DonorWithDistance> ResultsWithDistance { get; set; } = new();
}
