using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Notifico.Data;
using Notifico.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace Notifico.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<ProfileController> logger;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<AppUser> userManager,
            ILogger<ProfileController> logger,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.logger = logger;
            this._context = context;
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (IsAdmin())
            {
                logger.LogWarning("Admin rolündeki kullanıcı profil görüntülemeye çalıştı. User: {User}", User.Identity?.Name);
                return Forbid();
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("Kullanıcı profil görüntülerken bulunamadı. User: {User}", User.Identity?.Name);
                return NotFound();
            }

            logger.LogInformation("Kullanıcı profil sayfası görüntülendi. UserId: {UserId}", user.Id);

            var model = new ProfileViewModel
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            if (IsAdmin())
            {
                logger.LogWarning("Admin rolündeki kullanıcı profil güncelleme denedi. User: {User}", User.Identity?.Name);
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Profil güncelleme formu geçersiz gönderildi.");
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("Kullanıcı profil güncellerken bulunamadı. User: {User}", User.Identity?.Name);
                return NotFound();
            }

            if (user.UserName != model.UserName)
            {
                var setUserNameResult = await userManager.SetUserNameAsync(user, model.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        logger.LogWarning("Kullanıcı adı değişikliği hatası: {Error}", error.Description);
                    }
                    return View(model);
                }
                logger.LogInformation("Kullanıcı adı değiştirildi. Eski: {Old}, Yeni: {New}, UserId: {UserId}", user.UserName, model.UserName, user.Id);
            }

            if (user.Email != model.Email)
            {
                var setEmailResult = await userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        logger.LogWarning("Eposta değişikliği hatası: {Error}", error.Description);
                    }
                    return View(model);
                }
                logger.LogInformation("Kullanıcı eposta adresi değiştirildi. Eski: {Old}, Yeni: {New}, UserId: {UserId}", user.Email, model.Email, user.Id);
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.City = model.City;
            user.District = model.District;

            if (model.BirthDate.HasValue)
            {
                user.BirthDate = DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc);
            }
            else
            {
                user.BirthDate = null;
            }

            await userManager.UpdateAsync(user);

            var userId = user.Id;
            var defaultAddress = _context.Addresses.FirstOrDefault(a => a.UserId == userId && a.IsDefault);
            if (defaultAddress != null)
            {
                defaultAddress.FullAddress = model.Address;  
                defaultAddress.City = model.City;
                defaultAddress.District = model.District;
                await _context.SaveChangesAsync();
            }

            logger.LogInformation("Profil bilgileri güncellendi. UserId: {UserId}", user.Id);

            TempData["ProfileSuccess"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (IsAdmin())
            {
                logger.LogWarning("Admin rolündeki kullanıcı şifre değiştirme sayfasına ulaşmaya çalıştı.");
                return Forbid();
            }

            logger.LogInformation("Şifre değiştirme sayfası görüntülendi. User: {User}", User.Identity?.Name);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (IsAdmin())
            {
                logger.LogWarning("Admin rolündeki kullanıcı şifre değiştirmeye çalıştı.");
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Şifre değiştirme formu hatalı gönderildi.");
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("Şifre değiştirirken kullanıcı bulunamadı. User: {User}", User.Identity?.Name);
                return NotFound();
            }

            var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                logger.LogInformation("Kullanıcı şifresini değiştirdi. UserId: {UserId}", user.Id);
                TempData["Success"] = "Şifreniz başarıyla güncellendi";
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    logger.LogWarning("Şifre değiştirme hatası: {Error}", error.Description);
                }
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            if (IsAdmin())
            {
                logger.LogWarning("Admin rolündeki kullanıcı profil düzenleme sayfasına erişmeye çalıştı.");
                return Forbid();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                logger.LogWarning("Profil düzenleme için kullanıcı bulunamadı. UserId: {UserId}", userId);
                return NotFound();
            }

            logger.LogInformation("Profil düzenleme sayfası açıldı. UserId: {UserId}", user.Id);

            var model = new ProfileEditViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,
                Address = user.Address,
                City = user.City,
                District = user.District
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            if (IsAdmin())
            {
                logger.LogWarning("Admin rolündeki kullanıcı profil düzenleme denedi.");
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Profil düzenleme formu hatalı gönderildi.");
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                logger.LogWarning("Profil düzenlerken kullanıcı bulunamadı. UserId: {UserId}", userId);
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.City = model.City;
            user.District = model.District;

            if (model.BirthDate.HasValue)
            {
                user.BirthDate = DateTime.SpecifyKind(model.BirthDate.Value, DateTimeKind.Utc);
            }
            else
            {
                user.BirthDate = null;
            }

            await userManager.UpdateAsync(user);

            var defaultAddress = _context.Addresses.FirstOrDefault(a => a.UserId == userId && a.IsDefault);
            if (defaultAddress != null)
            {
                defaultAddress.FullAddress = model.Address; 
                defaultAddress.City = model.City;
                defaultAddress.District = model.District;
                await _context.SaveChangesAsync();
            }

            logger.LogInformation("Profil düzenleme tamamlandı. UserId: {UserId}", user.Id);

            TempData["ProfileSuccess"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction("Index");
        }
    }
}
