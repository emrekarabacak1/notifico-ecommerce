using Microsoft.AspNetCore.Identity;
using Notifico.Models;
using Notifico.Repositories;
using Notifico.Data;
using System.Threading.Tasks;

namespace Notifico.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly EmailHelper _emailHelper;

        public AccountService(
            IUserRepository userRepository,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ApplicationDbContext context,
            EmailHelper emailHelper)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailHelper = emailHelper;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterViewModel model)
        {
            var user = new AppUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                BirthDate = model.BirthDate.HasValue ? DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc) : null,
                City = model.City,
                District = model.District
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await AddDefaultAddressAsync(user, model);
            }

            return result;
        }

        public async Task<SignInResult> LoginAsync(LoginViewModel model)
        {
            var user = await _userRepository.FindByEmailAsync(model.Email);
            if (user == null || !await _userRepository.IsEmailConfirmedAsync(user))
                return SignInResult.Failed;

            return await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
        }

        public async Task LogoutAsync() => await _signInManager.SignOutAsync();

        public async Task<AppUser> FindByEmailAsync(string email) =>
            await _userRepository.FindByEmailAsync(email);

        public async Task<AppUser> FindByIdAsync(string userId) =>
            await _userRepository.FindByIdAsync(userId);

        public async Task<string> GenerateEmailConfirmationTokenAsync(AppUser user) =>
            await _userManager.GenerateEmailConfirmationTokenAsync(user);

        public async Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token) =>
            await _userManager.ConfirmEmailAsync(user, token);

        public async Task<bool> IsEmailConfirmedAsync(AppUser user) =>
            await _userRepository.IsEmailConfirmedAsync(user);

        public async Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string password) =>
            await _userManager.ResetPasswordAsync(user, token, password);

        public async Task<string> GeneratePasswordResetTokenAsync(AppUser user) =>
            await _userManager.GeneratePasswordResetTokenAsync(user);

        public async Task SendRegisterConfirmationMailAsync(AppUser user, string confirmLink)
        {
            var mailBody = $@"
                <h2>Notifico'ya Hoşgeldiniz!</h2>
                <p>Hesabınızı aktifleştirmek için <a href='{confirmLink}'>buraya tıklayın</a>.</p>
                <p>Link çalışmazsa: <br/><code>{confirmLink}</code></p>";
            await _emailHelper.SendEmailAsync(user.Email, "E-posta Onayı", mailBody);
        }

        public async Task SendPasswordResetMailAsync(string email, string resetLink)
        {
            var mailBody = $"<p>Şifrenizi sıfırlamak için <a href='{resetLink}'>buraya tıklayın</a>.</p>";
            await _emailHelper.SendEmailAsync(email, "Notifico Şifre Sıfırlama", mailBody);
        }

        public async Task AddDefaultAddressAsync(AppUser user, RegisterViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.Address) &&
                !string.IsNullOrWhiteSpace(model.City) &&
                !string.IsNullOrWhiteSpace(model.District))
            {
                var address = new Address
                {
                    UserId = user.Id,
                    Title = "Varsayılan Adres",
                    FullAddress = model.Address,
                    City = model.City,
                    District = model.District,
                    ZipCode = "",
                    IsDefault = true
                };
                await _userRepository.AddAddressAsync(address);
            }
        }
    }
}
