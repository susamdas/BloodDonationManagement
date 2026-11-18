using BloodDonationManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Data;

/// <summary>
/// Seeds FICTIONAL demo data (donors, requests, donation history) so the Smart Matching
/// feature can be demonstrated. Runs only when "SeedDemoData" is true and there are no donors yet.
/// All names and phone numbers are made up (phones use the 0170000xxxx pattern).
/// </summary>
public static class DemoDataSeeder
{
    public const string DemoPassword = "Demo@123";

    private record Area(string District, double Lat, double Lng, double SpreadDeg, int Count);

    // Approximate city centres; donors are scattered around them.
    private static readonly Area[] Areas =
    {
        new("Dhaka",       23.7800, 90.4000, 0.08, 28),
        new("Gazipur",     24.0000, 90.4200, 0.06, 6),
        new("Narayanganj", 23.6200, 90.5000, 0.04, 4),
        new("Chittagong",  22.3500, 91.8000, 0.06, 5),
        new("Sylhet",      24.8950, 91.8700, 0.04, 4),
        new("Rajshahi",    24.3700, 88.6000, 0.04, 3),
    };

    private static readonly string[] MaleNames =
    {
        "Rahim Uddin", "Karim Hossain", "Tanvir Ahmed", "Sabbir Rahman", "Mahfuz Alam", "Arif Chowdhury",
        "Imran Kabir", "Fahim Islam", "Nayeem Hasan", "Rakib Mia", "Shakil Ahmed", "Jubayer Hossain",
        "Mehedi Hasan", "Sohel Rana", "Tareq Aziz", "Anik Das", "Rifat Karim", "Asif Iqbal",
        "Zahid Hasan", "Polash Sarker", "Rony Barua", "Habib Ullah", "Ripon Sheikh", "Monir Hossain",
        "Shuvo Paul", "Kamal Uddin", "Faisal Mahmud", "Saiful Islam", "Biplob Roy", "Emon Khan",
        "Riad Hasan", "Touhid Alam"
    };

    private static readonly string[] FemaleNames =
    {
        "Nusrat Jahan", "Farzana Akter", "Sadia Islam", "Tania Sultana", "Rumana Khatun", "Mitu Rani",
        "Shirin Akter", "Lamia Haque", "Sharmin Nahar", "Tasnim Ara", "Jannatul Ferdous", "Puja Saha",
        "Afroza Begum", "Sumaiya Rahman", "Nadia Chowdhury", "Rima Das", "Ayesha Siddika", "Mousumi Akter"
    };

    // Roughly reflects blood group frequency in Bangladesh, with enough rare groups to demo.
    private static readonly string[] Groups =
    {
        "B+", "O+", "A+", "B+", "O+", "A+", "B+", "O+", "AB+", "B+",
        "O+", "A+", "O-", "B+", "O+", "A+", "B-", "B+", "O+", "AB+",
        "A+", "B+", "O+", "A-", "B+", "O+", "A+", "B+", "O+", "AB-",
        "B+", "O+", "A+", "O-", "B+", "O+", "A+", "B+", "AB+", "O+",
        "B+", "A+", "O+", "B-", "B+", "O+", "A+", "B+", "O+", "A+"
    };

    // Days since last donation (null = never). Mix of eligible, on cooldown, and long-rested donors.
    private static readonly int?[] DaysSince =
    {
        200, null, 30, 400, 100, 150, null, 60, 250, 95,
        110, 365, null, 45, 130, 220, 180, null, 15, 300,
        125, 90, null, 70, 500, 140, 100, 260, null, 35
    };

    public static async Task SeedAsync(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        if (await context.Donors.AnyAsync()) return;

        var rnd = new Random(42); // deterministic: same demo data on every PC
        var districts = await context.Districts.Include(d => d.Thanas).ToListAsync();

        var donors = new List<Donor>();
        int i = 0, male = 0, female = 0;
        foreach (var area in Areas)
        {
            var district = districts.FirstOrDefault(d => d.Name.Equals(area.District, StringComparison.OrdinalIgnoreCase));
            var thanas = district?.Thanas.OrderBy(t => t.Name).ToList() ?? new List<Thana>();

            for (var k = 0; k < area.Count; k++, i++)
            {
                var isFemale = i % 3 == 1; // about one third female
                var name = isFemale ? FemaleNames[female++ % FemaleNames.Length] : MaleNames[male++ % MaleNames.Length];
                var days = DaysSince[i % DaysSince.Length];

                donors.Add(new Donor
                {
                    FullName = name,
                    Gender = isFemale ? Gender.Female : Gender.Male,
                    BloodGroup = Groups[i % Groups.Length],
                    ContactNo = $"0170000{i + 1:0000}",
                    DistrictId = district?.DistrictId,
                    ThanaId = thanas.Count > 0 ? thanas[k % thanas.Count].ThanaId : null,
                    Latitude = Math.Round(area.Lat + (rnd.NextDouble() - 0.5) * 2 * area.SpreadDeg, 5),
                    Longitude = Math.Round(area.Lng + (rnd.NextDouble() - 0.5) * 2 * area.SpreadDeg, 5),
                    LastDonationDate = days.HasValue ? DateTime.Today.AddDays(-days.Value) : null,
                    IsAvailable = i % 11 != 5, // a few donors have paused alerts
                    Status = true,
                    CreatedAt = DateTime.Now.AddDays(-rnd.Next(30, 400))
                });
            }
        }
        context.Donors.AddRange(donors);
        await context.SaveChangesAsync();

        // Demo login accounts (fictional)
        var rahimUser = await EnsureUserAsync(userManager, "donor.rahim@example.com");
        var nusratUser = await EnsureUserAsync(userManager, "donor.nusrat@example.com");
        var requesterUser = await EnsureUserAsync(userManager, "requester@example.com");
        // Demo donor accounts are kept eligible so the Accept flow can be shown live:
        // Rahim (O+, matches the Critical O+ request) and Nusrat (rare B-, matches the thalassemia request).
        var rahim = donors.First(d => d.FullName == "Rahim Uddin");
        rahim.UserId = rahimUser.Id;
        rahim.BloodGroup = "O+";
        rahim.LastDonationDate = DateTime.Today.AddDays(-150);
        rahim.IsAvailable = true;
        var nusrat = donors.First(d => d.FullName == "Nusrat Jahan");
        nusrat.UserId = nusratUser.Id;
        nusrat.BloodGroup = "B-";
        nusrat.LastDonationDate = DateTime.Today.AddDays(-200);
        nusrat.IsAvailable = true;
        await context.SaveChangesAsync();

        var dhaka = districts.FirstOrDefault(d => d.Name == "Dhaka");
        var ctg = districts.FirstOrDefault(d => d.Name == "Chittagong");

        // Past (fulfilled) requests: build response history so the Reliability factor has data
        var past = new List<BloodRequest>();
        string[] pastGroups = { "B+", "O+", "A+", "B+", "O+", "AB+" };
        for (var p = 0; p < pastGroups.Length; p++)
        {
            past.Add(new BloodRequest
            {
                RequesterUserId = requesterUser.Id,
                PatientName = $"Past Patient {p + 1}",
                BloodGroup = pastGroups[p],
                ContactNo = $"0170009{p + 1:0000}",
                DistrictId = dhaka?.DistrictId,
                HospitalName = "Dhaka Medical College Hospital",
                Latitude = 23.7257, Longitude = 90.3976,
                Urgency = (UrgencyLevel)(p % 3),
                UnitsNeeded = 1,
                Status = RequestStatus.Fulfilled,
                IsVerified = true,
                CreatedDate = DateTime.Now.AddDays(-(20 + p * 25)),
                FulfilledAt = DateTime.Now.AddDays(-(20 + p * 25)).AddHours(3 + p)
            });
        }
        context.BloodRequests.AddRange(past);
        await context.SaveChangesAsync();

        var donatedViaRequest = new HashSet<int>();
        foreach (var req in past)
        {
            var compatible = donors
                .Where(d => d.UserId == null && d.DistrictId == req.DistrictId
                            && Services.BloodCompatibilityService.CanDonateToRecipient(d.BloodGroup, req.BloodGroup))
                .Take(5).ToList();
            var confirmedOne = false;
            foreach (var d in compatible)
            {
                var accepted = rnd.NextDouble() < 0.6;
                var status = accepted ? ResponseStatus.Accepted : ResponseStatus.Declined;
                if (accepted && !confirmedOne) { status = ResponseStatus.Confirmed; confirmedOne = true; }

                var notifiedAt = req.CreatedDate.AddMinutes(1);
                context.DonorResponses.Add(new DonorResponse
                {
                    BloodRequestId = req.BloodRequestId,
                    DonorId = d.DonorId,
                    Status = status,
                    MatchScore = 50 + rnd.Next(0, 45),
                    NotifiedAt = notifiedAt,
                    RespondedAt = notifiedAt.AddMinutes(rnd.Next(5, 90)),
                    ConfirmedAt = status == ResponseStatus.Confirmed ? req.FulfilledAt : null
                });
                if (status == ResponseStatus.Confirmed)
                {
                    var date = req.FulfilledAt!.Value.Date;
                    if (!d.LastDonationDate.HasValue || d.LastDonationDate < date) d.LastDonationDate = date;
                    donatedViaRequest.Add(d.DonorId);
                    context.Donations.Add(new Donation
                    {
                        DonorId = d.DonorId,
                        BloodRequestId = req.BloodRequestId,
                        DonationDate = req.FulfilledAt!.Value.Date,
                        HospitalName = req.HospitalName
                    });
                }
            }
        }

        // Donation history entries for donors whose last donation was not linked to a request
        foreach (var d in donors.Where(d => d.LastDonationDate.HasValue && !donatedViaRequest.Contains(d.DonorId)))
        {
            context.Donations.Add(new Donation
            {
                DonorId = d.DonorId,
                DonationDate = d.LastDonationDate!.Value,
                HospitalName = "Quantum Blood Bank (demo)"
            });
        }

        // Open requests to demo the Smart Matching page
        context.BloodRequests.AddRange(
            new BloodRequest
            {
                RequesterUserId = requesterUser.Id,
                PatientName = "Demo Patient — Accident case",
                BloodGroup = "O+",
                ContactNo = "01700099001",
                DistrictId = dhaka?.DistrictId,
                HospitalName = "Dhaka Medical College Hospital",
                Latitude = 23.7257, Longitude = 90.3976,
                Urgency = UrgencyLevel.Critical,
                UnitsNeeded = 2,
                NeededBy = DateTime.Now.AddHours(6),
                Message = "Emergency surgery. Please come to the blood bank, ground floor.",
                IsVerified = true,
                CreatedDate = DateTime.Now.AddMinutes(-40)
            },
            new BloodRequest
            {
                RequesterUserId = requesterUser.Id,
                PatientName = "Demo Patient — Thalassemia",
                BloodGroup = "B-",
                ContactNo = "01700099002",
                DistrictId = dhaka?.DistrictId,
                HospitalName = "Bangabandhu Sheikh Mujib Medical University",
                Latitude = 23.7389, Longitude = 90.3952,
                Urgency = UrgencyLevel.Urgent,
                UnitsNeeded = 1,
                NeededBy = DateTime.Now.AddDays(2),
                Message = "Regular transfusion for a thalassemia patient.",
                CreatedDate = DateTime.Now.AddHours(-3)
            },
            new BloodRequest
            {
                RequesterUserId = requesterUser.Id,
                PatientName = "Demo Patient — Delivery",
                BloodGroup = "A+",
                ContactNo = "01700099003",
                DistrictId = ctg?.DistrictId,
                HospitalName = "Chittagong Medical College Hospital",
                Latitude = 22.3597, Longitude = 91.8317,
                Urgency = UrgencyLevel.Normal,
                UnitsNeeded = 1,
                NeededBy = DateTime.Now.AddDays(4),
                CreatedDate = DateTime.Now.AddHours(-8)
            });

        await context.SaveChangesAsync();
    }

    private static async Task<IdentityUser> EnsureUserAsync(UserManager<IdentityUser> userManager, string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null) return user;

        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Demo user creation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "User");
        return user;
    }
}
