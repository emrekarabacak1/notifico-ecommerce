using Notifico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Notifico.Repositories
{
    public interface IUserRepository
    {
        Task<AppUser> FindByEmailAsync(string email);
        Task<AppUser> FindByIdAsync(string userId);
        Task<AppUser> FindByUserNameAsync(string userName);
        Task<bool> IsEmailConfirmedAsync(AppUser user);
        Task<IList<string>> GetRolesAsync(AppUser user);
        Task AddAddressAsync(Address address);
        Task<List<Address>> GetUserAddressesAsync(string userId);
        Task<IdentityResult> UpdateUserAsync(AppUser user);
        Task<IdentityResult> SetUserNameAsync(AppUser user, string userName);
        Task<IdentityResult> SetEmailAsync(AppUser user, string email);
        Task<bool> UserNameExistsAsync(string userName, string exceptUserId);
        Task<IdentityResult> ChangePasswordAsync(AppUser user, string oldPassword, string newPassword);
        Task UpdateAddressAsync(Address address);
        Task<Address> GetDefaultAddressAsync(string userId);
    }
}
