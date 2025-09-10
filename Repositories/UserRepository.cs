using Microsoft.AspNetCore.Identity;
using Notifico.Data;
using Notifico.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Notifico.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserRepository(UserManager<AppUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        public async Task<AppUser> FindByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<AppUser> FindByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<AppUser> FindByUserNameAsync(string userName)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task<bool> IsEmailConfirmedAsync(AppUser user)
        {
            return await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IList<string>> GetRolesAsync(AppUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task AddAddressAsync(Address address)
        {
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Address>> GetUserAddressesAsync(string userId)
        {
            return await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }
        public async Task<IdentityResult> UpdateUserAsync(AppUser user)
        {
            return await _userManager.UpdateAsync(user);
        }
        public async Task<IdentityResult> SetUserNameAsync(AppUser user, string userName)
        {
            return await _userManager.SetUserNameAsync(user, userName);
        }
        public async Task<IdentityResult> SetEmailAsync(AppUser user, string email)
        {
            return await _userManager.SetEmailAsync(user, email);
        }
        public async Task<bool> UserNameExistsAsync(string userName, string exceptUserId)
        {
            return await _userManager.Users
                .AnyAsync(u => u.UserName == userName && u.Id != exceptUserId);
        }
        public async Task<IdentityResult> ChangePasswordAsync(AppUser user, string oldPassword, string newPassword)
        {
            return await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        }
        public async Task UpdateAddressAsync(Address address)
        {
            _context.Addresses.Update(address);
            await _context.SaveChangesAsync();
        }
        public async Task<Address> GetDefaultAddressAsync(string userId)
        {
            return await _context.Addresses
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
        }
    }
}
