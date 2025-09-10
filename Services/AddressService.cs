using Notifico.Models;
using Notifico.Repositories;
using Microsoft.EntityFrameworkCore;
using Notifico.Data;

namespace Notifico.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ApplicationDbContext _context; 

        public AddressService(IAddressRepository addressRepository, ApplicationDbContext context)
        {
            _addressRepository = addressRepository;
            _context = context;
        }

        public async Task<List<Address>> GetUserAddressesAsync(string userId)
            => await _addressRepository.GetUserAddressesAsync(userId);

        public async Task<Address?> GetByIdAsync(int id, string userId)
            => await _addressRepository.GetByIdAsync(id, userId);

        public async Task<bool> AddAddressAsync(Address address)
        {
            var userId = address.UserId;
            if (address.IsDefault)
                await _addressRepository.SetDefaultAddressAsync(userId, 0); 

            await _addressRepository.AddAsync(address);
            await _addressRepository.SaveChangesAsync();

            if (address.IsDefault)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.Address = address.FullAddress;
                    user.City = address.City;
                    user.District = address.District;
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task<bool> UpdateAddressAsync(Address address, string userId)
        {
            var addressDb = await _addressRepository.GetByIdAsync(address.Id, userId);
            if (addressDb == null) return false;

            addressDb.Title = address.Title;
            addressDb.FullAddress = address.FullAddress;
            addressDb.City = address.City;
            addressDb.District = address.District;
            addressDb.ZipCode = address.ZipCode;
            if (address.IsDefault)
                await _addressRepository.SetDefaultAddressAsync(userId, address.Id);

            addressDb.IsDefault = address.IsDefault;

            await _addressRepository.SaveChangesAsync();

            if (addressDb.IsDefault)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.Address = addressDb.FullAddress;
                    user.City = addressDb.City;
                    user.District = addressDb.District;
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task<bool> DeleteAddressAsync(int id, string userId)
        {
            var address = await _addressRepository.GetByIdAsync(id, userId);
            if (address == null) return false;

            if (await _addressRepository.IsAddressUsedInOrdersAsync(address.Id))
                return false;

            await _addressRepository.DeleteAsync(address);
            await _addressRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetDefaultAddressAsync(int id, string userId)
        {
            var addresses = await _addressRepository.GetUserAddressesAsync(userId);
            var selectedDefault = addresses.FirstOrDefault(a => a.Id == id);
            if (selectedDefault == null) return false;

            await _addressRepository.SetDefaultAddressAsync(userId, id);
            await _addressRepository.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Address = selectedDefault.FullAddress;
                user.City = selectedDefault.City;
                user.District = selectedDefault.District;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<Address?> GetDefaultAddressAsync(string userId)
            => await _addressRepository.GetDefaultAddressAsync(userId);

        public async Task<bool> IsAddressUsedInOrdersAsync(int addressId)
            => await _addressRepository.IsAddressUsedInOrdersAsync(addressId);
    }
}
