using Notifico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifico.Repositories
{
    public interface IAddressRepository
    {
        Task<List<Address>> GetUserAddressesAsync(string userId);
        Task<Address?> GetByIdAsync(int id, string userId);
        Task<Address?> GetByIdAsync(int id);
        Task AddAsync(Address address);
        Task UpdateAsync(Address address);
        Task DeleteAsync(Address address);
        Task SetDefaultAddressAsync(string userId, int defaultAddressId);
        Task<Address?> GetDefaultAddressAsync(string userId);
        Task<bool> IsAddressUsedInOrdersAsync(int addressId);
        Task SaveChangesAsync();
    }
}
