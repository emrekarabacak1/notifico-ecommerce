using Notifico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Notifico.Services
{
    public interface IProfileService
    {
        Task<AppUser> GetUserByIdAsync(string userId);
        Task<ProfileViewModel> GetProfileViewModelAsync(string userId);
        Task<IdentityResult> UpdateProfileAsync(string userId, ProfileViewModel model);
        Task<bool> UserNameExistsAsync(string userName, string exceptUserId);
        Task<IdentityResult> ChangePasswordAsync(string userId, string oldPassword, string newPassword);

        Task<ProfileEditViewModel> GetProfileEditViewModelAsync(string userId);
        Task<IdentityResult> UpdateProfileEditAsync(string userId, ProfileEditViewModel model);
        Task<Address> GetDefaultAddressAsync(string userId);
        Task UpdateDefaultAddressAsync(string userId, string address, string city, string district);
    }
}
