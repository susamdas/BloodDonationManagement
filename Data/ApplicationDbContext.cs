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
                .WithMany()
                .HasForeignKey(d => d.ThanaId)
                .OnDelete(DeleteBehavior.SetNull); // Cascade er bajaye SetNull

            base.OnModelCreating(modelBuilder);
        }




    }
}
