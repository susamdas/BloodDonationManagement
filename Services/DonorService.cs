using BloodDonationManagement.Models;
using BloodDonationManagement.Repositories;

namespace BloodDonationManagement.Services
{
    public class DonorService
    {
        private readonly DonorRepository _donorRepository;

        public DonorService(DonorRepository donorRepository)
        {
            _donorRepository = donorRepository;
        }

        public async Task AddDonorAsync(Donor donor)
        {
            await _donorRepository.AddAsync(donor);
        }
    }
}
