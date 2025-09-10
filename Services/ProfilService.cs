using Notifico.Models;
using Notifico.Repositories;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace Notifico.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;

        public ProfileService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AppUser> GetUserByIdAsync(string userId)
        {
            return await _userRepository.FindByIdAsync(userId);
        }

        public async Task<ProfileViewModel> GetProfileViewModelAsync(string userId)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null) return null;

            return new ProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,
                Address = user.Address,
                City = user.City,
                District = user.District
            };
        }

        public async Task<IdentityResult> UpdateProfileAsync(string userId, ProfileViewModel model)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "Kullanıcı bulunamadı." });

            if (user.UserName != model.UserName)
            {
                var exists = await _userRepository.UserNameExistsAsync(model.UserName, userId);
                if (exists)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "Bu kullanıcı adı zaten alınmış." });
                }
                var result = await _userRepository.SetUserNameAsync(user, model.UserName);
                if (!result.Succeeded) return result;
            }

            if (user.Email != model.Email)
            {
                var result = await _userRepository.SetEmailAsync(user, model.Email);
                if (!result.Succeeded) return result;
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.City = model.City;
            user.District = model.District;
            user.BirthDate = model.BirthDate.HasValue
                ? DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc)
                : null;

            return await _userRepository.UpdateUserAsync(user);
        }

        public async Task<bool> UserNameExistsAsync(string userName, string exceptUserId)
        {
            return await _userRepository.UserNameExistsAsync(userName, exceptUserId);
        }

        public async Task<IdentityResult> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "Kullanıcı bulunamadı." });

            return await _userRepository.ChangePasswordAsync(user, oldPassword, newPassword);
        }

        public async Task<ProfileEditViewModel> GetProfileEditViewModelAsync(string userId)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null) return null;

            return new ProfileEditViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,
                Address = user.Address,
                City = user.City,
                District = user.District
            };
        }

        public async Task<IdentityResult> UpdateProfileEditAsync(string userId, ProfileEditViewModel model)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "Kullanıcı bulunamadı." });

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.City = model.City;
            user.District = model.District;
            user.BirthDate = model.BirthDate.HasValue
                ? DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc)
                : null;

            return await _userRepository.UpdateUserAsync(user);
        }

        public async Task<Address> GetDefaultAddressAsync(string userId)
        {
            return await _userRepository.GetDefaultAddressAsync(userId);
        }

        public async Task UpdateDefaultAddressAsync(string userId, string address, string city, string district)
        {
            var defaultAddress = await _userRepository.GetDefaultAddressAsync(userId);
            if (defaultAddress != null)
            {
                defaultAddress.FullAddress = address;
                defaultAddress.City = city;
                defaultAddress.District = district;
                await _userRepository.UpdateAddressAsync(defaultAddress);
            }
        }
    }
}
