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
        public DbSet<Union> Unions { get; set; }   // ✅ New table added

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Donor → Thana (nullable)
            modelBuilder.Entity<Donor>()
                .HasOne(d => d.Thana)
                .WithMany()
                .HasForeignKey(d => d.ThanaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Thana → District (1:N)
            modelBuilder.Entity<Thana>()
                .HasOne(t => t.District)
                .WithMany(d => d.Thanas) // District has many Thanas
                .HasForeignKey(t => t.DistrictId    )
                .OnDelete(DeleteBehavior.Restrict);

            // Union → Thana (1:N)
            modelBuilder.Entity<Union>()
                .HasOne(u => u.Thana)
                .WithMany(t => t.Unions) // Thana has many Unions
                .HasForeignKey(u => u.ThanaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
