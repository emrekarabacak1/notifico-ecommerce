using Microsoft.AspNetCore.Identity;
using Notifico.Models;
using System.Threading.Tasks;

namespace Notifico.Services
{
    public interface IAccountService
    {
        Task<IdentityResult> RegisterAsync(RegisterViewModel model);
        Task<SignInResult> LoginAsync(LoginViewModel model);
        Task LogoutAsync();
        Task<AppUser> FindByEmailAsync(string email);
        Task<AppUser> FindByIdAsync(string userId);
        Task<string> GenerateEmailConfirmationTokenAsync(AppUser user);
        Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token);
        Task<bool> IsEmailConfirmedAsync(AppUser user);
        Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string password);
        Task<string> GeneratePasswordResetTokenAsync(AppUser user);
        Task SendRegisterConfirmationMailAsync(AppUser user, string confirmLink);
        Task SendPasswordResetMailAsync(string email, string resetLink);
        Task AddDefaultAddressAsync(AppUser user, RegisterViewModel model);
    }
}
