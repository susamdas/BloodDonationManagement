using BloodDonationManagement.Data;
using BloodDonationManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Repositories
{
    public class DonorRepository
    {
        private readonly ApplicationDbContext _context;

        public DonorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Donor donor)
        {
            _context.Donors.Add(donor);
            await _context.SaveChangesAsync();
        }
    }
}