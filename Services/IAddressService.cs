using Notifico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifico.Services
{
    public interface IAddressService
    {
        Task<List<Address>> GetUserAddressesAsync(string userId);
        Task<Address?> GetByIdAsync(int id, string userId);
        Task<bool> AddAddressAsync(Address address);
        Task<bool> UpdateAddressAsync(Address address, string userId);
        Task<bool> DeleteAddressAsync(int id, string userId);
        Task<bool> SetDefaultAddressAsync(int id, string userId);
        Task<Address?> GetDefaultAddressAsync(string userId);
        Task<bool> IsAddressUsedInOrdersAsync(int addressId);
    }
}
