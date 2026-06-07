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

            modelBuilder.Entity<Thana>()
                .HasOne(t => t.District)
                .WithMany(district => district.Thanas)
                .HasForeignKey(t => t.DistrictId)
                .OnDelete(DeleteBehavior.NoAction);

            base.OnModelCreating(modelBuilder);
        }




    }
}
