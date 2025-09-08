using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notifico.Data;
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
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            EmailHelper emailHelper,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailHelper = emailHelper;
            _logger = logger;
            _context = context;
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
                TempData["Error"] = "Bu e-posta ile zaten kayıt olunmuş.";
                return View(model);
            }
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                TempData["Error"] = "Bu kullanıcı adı zaten alınmış.";
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
                BirthDate = model.BirthDate.HasValue ? DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc) : null,
                City = model.City,
                District = model.District
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

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

                    _context.Addresses.Add(address);
                    await _context.SaveChangesAsync();
                }

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

                    TempData["Info"] = "Kayıt başarılı! E-posta adresinize bir onay maili gönderildi.";
                    TempData["EmailConfirmationLink"] = confirmLink;
                }
                catch (Exception)
                {
                    await _userManager.DeleteAsync(user);
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
        public IActionResult RegisterConfirmation()
        {
            if (TempData["Info"] == null)
                TempData["Info"] = "Lütfen e-posta adresinizi kontrol ederek hesabınızı onaylayın.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "E-posta onay linki hatalı veya eksik.";
                return View("ConfirmEmailError");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "E-posta onayında kullanıcı bulunamadı.";
                return View("ConfirmEmailError");
            }

            token = System.Net.WebUtility.UrlDecode(token);

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["Success"] = "E-posta onayı başarılı! Artık giriş yapabilirsiniz.";
                return View("ConfirmEmailSuccess");
            }
            else
            {
                TempData["Error"] = "E-posta onayında bir hata oluştu.";
                return View("ConfirmEmailError");
            }
        }

        [HttpGet]
        public IActionResult ConfirmEmailSuccess()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ConfirmEmailError()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("", "E-posta adresi bulunamadı ya da onaylanmamış.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            if (result.Succeeded)
            {
                TempData["Success"] = "Giriş başarılı!";
                return RedirectToAction("Index", "Product");
            }

            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Info"] = "Çıkış yapıldı.";
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
                TempData["Error"] = "Lütfen geçerli bir e-posta giriniz.";
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Info"] = "Eğer bu e-posta adresi kayıtlıysa şifre sıfırlama maili gönderildi.";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);

            try
            {
                var mailBody = $"<p>Şifrenizi sıfırlamak için <a href='{link}'>buraya tıklayın</a>.</p>";
                await _emailHelper.SendEmailAsync(model.Email, "Notifico Şifre Sıfırlama", mailBody);

                TempData["Success"] = "Şifre sıfırlama maili gönderildi!";
            }
            catch (Exception)
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

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Error"] = "Şifre sıfırlama başarısız.";
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Şifre başarıyla sıfırlandı. Giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            foreach (var error in result.Errors)
                TempData["Error"] = error.Description;

            return View(model);
        }
    }
}
