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
            _logger.LogInformation("Kayıt sayfası görüntülendi. IP: {Ip}", HttpContext.Connection.RemoteIpAddress);
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
                _logger.LogWarning("Kayıt denemesi başarısız: Email zaten kayıtlı. Email: {Email}", model.Email);
                ModelState.AddModelError("Email", "Bu e-posta ile zaten kayıt olunmuş.");
                return View(model);
            }
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                _logger.LogWarning("Kayıt denemesi başarısız: Kullanıcı adı zaten alınmış. UserName: {UserName}", model.UserName);
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
                BirthDate = model.BirthDate.HasValue ? DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc) : null,
                City = model.City,
                District = model.District
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Yeni kullanıcı kaydı başarılı. UserId: {UserId}, UserName: {UserName}, Email: {Email}", user.Id, user.UserName, user.Email);
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
                    _logger.LogInformation("Varsayılan adres oluşturuldu. UserId: {UserId}", user.Id);
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
                    _logger.LogInformation("Kullanıcıya e-posta onay maili gönderildi. Email: {Email}", user.Email);

                    TempData["EmailConfirmationLink"] = confirmLink;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "E-posta gönderilemedi, kullanıcı siliniyor. Email: {Email}", user.Email);
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError("", "Kayıt sırasında e-posta gönderilemedi. Lütfen geçerli bir adres kullanın.");
                    return View(model);
                }
                return RedirectToAction("RegisterConfirmation");
            }
            foreach (var error in result.Errors)
            {
                _logger.LogWarning("Kullanıcı kaydı hatası: {Error} | UserName: {UserName}, Email: {Email}", error.Description, model.UserName, model.Email);
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            _logger.LogInformation("Kayıt başarılı, onay bekleniyor.");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("E-posta onay linki hatalı veya eksik.");
                return View("ConfirmEmailError");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("E-posta onayında kullanıcı bulunamadı. UserId: {UserId}", userId);
                return View("ConfirmEmailError");
            }

            token = System.Net.WebUtility.UrlDecode(token);

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("E-posta onayı başarılı. UserId: {UserId}", userId);
                return View("ConfirmEmailSuccess");
            }
            else
            {
                _logger.LogWarning("E-posta onayında hata. UserId: {UserId}", userId);
                return View("ConfirmEmailError");
            }
        }

        [HttpGet]
        public IActionResult ConfirmEmailSuccess()
        {
            _logger.LogInformation("Kullanıcı e-posta onayını başarılı tamamladı.");
            return View();
        }

        [HttpGet]
        public IActionResult ConfirmEmailError()
        {
            _logger.LogWarning("Kullanıcı e-posta onayını başarısız tamamladı.");
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            _logger.LogInformation("Login sayfası görüntülendi.");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Boş e-posta veya şifre ile giriş denemesi.");
                ViewBag.Error = "E-posta ve şifre gereklidir.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogWarning("Kullanıcı bulunamadı ya da email onaylanmamış. Email: {Email}", email);
                ViewBag.Error = "E-posta adresi bulunamadı ya da onaylanmamış.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
            if (result.Succeeded)
            {
                _logger.LogInformation("Giriş başarılı. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
                return RedirectToAction("Index", "Product");
            }

            _logger.LogWarning("Giriş başarısız. Email: {Email}", email);
            ViewBag.Error = "E-posta veya şifre hatalı.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name ?? "Anonim";
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Kullanıcı çıkış yaptı. User: {UserName}", userName);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            _logger.LogInformation("Şifremi unuttum sayfası görüntülendi.");
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
                _logger.LogWarning("Şifre sıfırlama isteği: kullanıcı yok veya email onaylanmamış. Email: {Email}", model.Email);
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);

            try
            {
                var mailBody = $"<p>Şifrenizi sıfırlamak için <a href='{link}'>buraya tıklayın</a>.</p>";
                await _emailHelper.SendEmailAsync(model.Email, "Notifico Şifre Sıfırlama", mailBody);

                _logger.LogInformation("Şifre sıfırlama maili gönderildi. Email: {Email}", model.Email);

                TempData["ResetPasswordLink"] = link;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi. Email: {Email}", model.Email);
            }

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Şifre sıfırlama linki hatalı veya eksik.");
                return RedirectToAction("Index", "Home");
            }

            _logger.LogInformation("Şifre sıfırlama formu açıldı. Email: {Email}", email);
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
                _logger.LogWarning("Şifre sıfırlama başarısız: kullanıcı bulunamadı. Email: {Email}", model.Email);
                TempData["ResetPasswordResult"] = "Şifre sıfırlama başarısız.";
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Şifre başarıyla sıfırlandı. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
                TempData["ResetPasswordResult"] = "Şifre başarıyla sıfırlandı. Giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            foreach (var error in result.Errors)
            {
                _logger.LogWarning("Şifre sıfırlama hatası: {Error} | UserId: {UserId}, Email: {Email}", error.Description, user.Id, user.Email);
                ModelState.AddModelError("", error.Description);
            }

            TempData["ResetPasswordResult"] = "Şifre sıfırlama başarısız.";
            return View(model);
        }
    }
}
