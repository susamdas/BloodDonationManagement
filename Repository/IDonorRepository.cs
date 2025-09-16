using BloodDonationManagement.Models;

namespace BloodDonationManagement.Repositories
{
    public interface IDonorRepository
    {
        Task AddAsync(Donor donor);
    }
}
