using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Notifico.Models;
using System.Security.Claims;

namespace Notifico.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly EmailHelper _emailHelper;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            EmailHelper emailHelper,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailHelper = emailHelper;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta ile zaten kayıt olunmuş.");
                return View(model);
            }
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten alınmış.");
                return View(model);
            }

            var user = new AppUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                BirthDate = model.BirthDate.HasValue ? DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc) : null
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = System.Net.WebUtility.UrlEncode(token);

                var confirmLink = Url.Action("ConfirmEmail", "Account",
                    new { userId = user.Id, token = encodedToken },
                    protocol: HttpContext.Request.Scheme);

                try
                {
                    var mailBody = $@"
                <h2>Notifico'ya Hoşgeldiniz!</h2>
                <p>Hesabınızı aktifleştirmek için <a href='{confirmLink}'>buraya tıklayın</a>.</p>
                <p>Link çalışmazsa: <br/><code>{confirmLink}</code></p>";
                    await _emailHelper.SendEmailAsync(user.Email, "E-posta Onayı", mailBody);

                    TempData["EmailConfirmationLink"] = confirmLink;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "E-posta gönderilemedi, kullanıcı siliniyor.");
                    _logger.LogError("EXCEPTION DETAIL: {Detail}", ex.ToString());

                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError("", "Kayıt sırasında e-posta gönderilemedi. Lütfen geçerli bir adres kullanın.");
                    return View(model);
                }
                return RedirectToAction("RegisterConfirmation");
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }


        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return View("ConfirmEmailError");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return View("ConfirmEmailError");

            token = System.Net.WebUtility.UrlDecode(token);

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
                return View("ConfirmEmailSuccess");
            else
                return View("ConfirmEmailError");
        }

        [HttpGet]
        public IActionResult ConfirmEmailSuccess() => View();

        [HttpGet]
        public IActionResult ConfirmEmailError() => View();

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "E-posta ve şifre gereklidir.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                ViewBag.Error = "E-posta adresi bulunamadı ya da onaylanmamış.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
            if (result.Succeeded)
                return RedirectToAction("Index", "Product");

            ViewBag.Error = "E-posta veya şifre hatalı.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            TempData["ResetPasswordInfo"] = "Eğer bu e-posta adresi kayıtlıysa şifre sıfırlama maili gönderildi.";

            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);

            try
            {
                var mailBody = $"<p>Şifrenizi sıfırlamak için <a href='{link}'>buraya tıklayın</a>.</p>";
                await _emailHelper.SendEmailAsync(model.Email, "Notifico Şifre Sıfırlama", mailBody);

                TempData["ResetPasswordLink"] = link;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi.");
                _logger.LogError("EXCEPTION DETAIL: {Detail}", ex.ToString());
            }

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
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
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["ResetPasswordResult"] = "Şifre sıfırlama başarısız.";
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["ResetPasswordResult"] = "Şifre başarıyla sıfırlandı. Giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            TempData["ResetPasswordResult"] = "Şifre sıfırlama başarısız.";
            return View(model);
        }
    }
}
