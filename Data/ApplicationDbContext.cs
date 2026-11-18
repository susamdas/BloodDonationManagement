using BloodDonationManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Donor> Donors { get; set; }
        public DbSet<BloodRequest> BloodRequests { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Thana> Thanas { get; set; }
        public DbSet<DonorResponse> DonorResponses { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RequestReport> RequestReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Donor>()
                .HasOne(d => d.Thana)
                .WithMany(t => t.Donors)
                .HasForeignKey(d => d.ThanaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Donor>()
                .HasOne(d => d.District)
                .WithMany(district => district.Donors)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.UserId);

            modelBuilder.Entity<Thana>()
                .HasOne(t => t.District)
                .WithMany(district => district.Thanas)
                .HasForeignKey(t => t.DistrictId)
                .OnDelete(DeleteBehavior.NoAction);

            // One response record per donor per request
            modelBuilder.Entity<DonorResponse>()
                .HasIndex(r => new { r.BloodRequestId, r.DonorId })
                .IsUnique();

            modelBuilder.Entity<DonorResponse>()
                .HasOne(r => r.BloodRequest)
                .WithMany(b => b.Responses)
                .HasForeignKey(r => r.BloodRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DonorResponse>()
                .HasOne(r => r.Donor)
                .WithMany(d => d.Responses)
                .HasForeignKey(r => r.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Donor)
                .WithMany(d => d.Donations)
                .HasForeignKey(d => d.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.BloodRequest)
                .WithMany()
                .HasForeignKey(d => d.BloodRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            modelBuilder.Entity<RequestReport>()
                .HasOne(r => r.BloodRequest)
                .WithMany()
                .HasForeignKey(r => r.BloodRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
