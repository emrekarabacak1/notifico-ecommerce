using Notifico.Data;
using Notifico.Models;
using Microsoft.EntityFrameworkCore;

namespace Notifico.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly ApplicationDbContext _context;
        public AddressRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Address>> GetUserAddressesAsync(string userId)
        {
            return await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();
        }

        public async Task<Address?> GetByIdAsync(int id, string userId)
        {
            return await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        }

        public async Task<Address?> GetByIdAsync(int id)
        {
            return await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddAsync(Address address)
        {
            await _context.Addresses.AddAsync(address);
        }

        public async Task UpdateAsync(Address address)
        {
            _context.Addresses.Update(address);
        }

        public async Task DeleteAsync(Address address)
        {
            _context.Addresses.Remove(address);
        }

        public async Task SetDefaultAddressAsync(string userId, int defaultAddressId)
        {
            var addresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
            foreach (var address in addresses)
            {
                address.IsDefault = address.Id == defaultAddressId;
            }
        }

        public async Task<Address?> GetDefaultAddressAsync(string userId)
        {
            return await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
        }

        public async Task<bool> IsAddressUsedInOrdersAsync(int addressId)
        {
            return await _context.Orders.AnyAsync(o => o.AddressId == addressId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
