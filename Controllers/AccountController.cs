using Microsoft.AspNetCore.Mvc;
using Notifico.Models;
using Notifico.Services;
using Notifico.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Notifico.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAccountService accountService,
            IUserRepository userRepository,
            ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _accountService.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                TempData["Error"] = "Bu e-posta ile zaten kayıt olunmuş.";
                return View(model);
            }

            var result = await _accountService.RegisterAsync(model);
            if (result.Succeeded)
            {
                var user = await _accountService.FindByEmailAsync(model.Email);

                var token = await _accountService.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = System.Net.WebUtility.UrlEncode(token);
                var confirmLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = encodedToken }, Request.Scheme);

                try
                {
                    await _accountService.SendRegisterConfirmationMailAsync(user, confirmLink);
                    TempData["Info"] = "Kayıt başarılı! E-posta adresinize bir onay maili gönderildi.";
                    TempData["EmailConfirmationLink"] = confirmLink;
                }
                catch
                {
                    TempData["Error"] = "Kayıt sırasında e-posta gönderilemedi. Lütfen geçerli bir adres kullanın.";
                    return View(model);
                }
                return RedirectToAction("RegisterConfirmation");
            }

            foreach (var error in result.Errors)
                TempData["Error"] = error.Description;

            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation() => View();

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "E-posta onay linki hatalı veya eksik.";
                return View("ConfirmEmailError");
            }

            var user = await _accountService.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "E-posta onayında kullanıcı bulunamadı.";
                return View("ConfirmEmailError");
            }

            token = System.Net.WebUtility.UrlDecode(token);
            var result = await _accountService.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                TempData["Success"] = "E-posta onayı başarılı! Artık giriş yapabilirsiniz.";
                return View("ConfirmEmailSuccess");
            }

            TempData["Error"] = "E-posta onayında bir hata oluştu.";
            return View("ConfirmEmailError");
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.LoginAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = "Giriş başarılı!";
                var user = await _accountService.FindByEmailAsync(model.Email);
                var roles = await _userRepository.GetRolesAsync(user);

                if (roles.Contains("Admin"))
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            HttpContext.Session.Clear();
            TempData.Clear();
            TempData["Info"] = "Çıkış yapıldı.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Lütfen geçerli bir e-posta giriniz.";
                return View();
            }

            var user = await _accountService.FindByEmailAsync(model.Email);
            if (user == null || !await _accountService.IsEmailConfirmedAsync(user))
            {
                TempData["Info"] = "Eğer bu e-posta adresi kayıtlıysa şifre sıfırlama maili gönderildi.";
                return View();
            }

            var token = await _accountService.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);

            try
            {
                await _accountService.SendPasswordResetMailAsync(model.Email, link);
                TempData["Success"] = "Şifre sıfırlama maili gönderildi!";
            }
            catch
            {
                TempData["Error"] = "Şifre sıfırlama e-postası gönderilemedi.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Şifre sıfırlama linki hatalı veya eksik.";
                return RedirectToAction("Index", "Home");
            }

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Bilgiler eksik.";
                return View(model);
            }

            var user = await _accountService.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Error"] = "Şifre sıfırlama başarısız.";
                return View(model);
            }

            var result = await _accountService.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Şifre başarıyla sıfırlandı. Giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                TempData["Error"] = error.Description;

            return View(model);
        }

        [HttpGet]
        public IActionResult ConfirmEmailSuccess() => View();

        [HttpGet]
        public IActionResult ConfirmEmailError() => View();
    }
}
