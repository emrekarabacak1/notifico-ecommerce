using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Notifico.Models;
using Notifico.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Notifico.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        private bool IsAdmin() => User.IsInRole("Admin");
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (IsAdmin())
            {
                _logger.LogWarning("Admin rolündeki kullanıcı profil görüntülemeye çalıştı. User: {User}", User.Identity?.Name);
                return Forbid();
            }

            var userId = GetUserId();
            var model = await _profileService.GetProfileViewModelAsync(userId);

            if (model == null)
            {
                _logger.LogWarning("Kullanıcı profil görüntülerken bulunamadı. User: {User}", User.Identity?.Name);
                return NotFound();
            }

            _logger.LogInformation("Kullanıcı profil sayfası görüntülendi. UserId: {UserId}", userId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            if (IsAdmin())
            {
                _logger.LogWarning("Admin rolündeki kullanıcı profil güncelleme denedi. User: {User}", User.Identity?.Name);
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Profil güncelleme formu geçersiz gönderildi.");
                return View(model);
            }

            var userId = GetUserId();
            var result = await _profileService.UpdateProfileAsync(userId, model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    _logger.LogWarning("Profil güncelleme hatası: {Error}", error.Description);
                }
                return View(model);
            }

            await _profileService.UpdateDefaultAddressAsync(userId, model.Address, model.City, model.District);

            _logger.LogInformation("Profil bilgileri güncellendi. UserId: {UserId}", userId);
            TempData["ProfileSuccess"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (IsAdmin())
            {
                _logger.LogWarning("Admin rolündeki kullanıcı şifre değiştirme sayfasına ulaşmaya çalıştı.");
                return Forbid();
            }

            _logger.LogInformation("Şifre değiştirme sayfası görüntülendi. User: {User}", User.Identity?.Name);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (IsAdmin())
            {
                _logger.LogWarning("Admin rolündeki kullanıcı şifre değiştirmeye çalıştı.");
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Şifre değiştirme formu hatalı gönderildi.");
                return View(model);
            }

            var userId = GetUserId();
            var result = await _profileService.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Kullanıcı şifresini değiştirdi. UserId: {UserId}", userId);
                TempData["Success"] = "Şifreniz başarıyla güncellendi";
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    _logger.LogWarning("Şifre değiştirme hatası: {Error}", error.Description);
                }
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            if (IsAdmin())
            {
                _logger.LogWarning("Admin rolündeki kullanıcı profil düzenleme sayfasına erişmeye çalıştı.");
                return Forbid();
            }

            var userId = GetUserId();
            var model = await _profileService.GetProfileEditViewModelAsync(userId);

            if (model == null)
            {
                _logger.LogWarning("Profil düzenleme için kullanıcı bulunamadı. UserId: {UserId}", userId);
                return NotFound();
            }

            _logger.LogInformation("Profil düzenleme sayfası açıldı. UserId: {UserId}", userId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            if (IsAdmin())
            {
                _logger.LogWarning("Admin rolündeki kullanıcı profil düzenleme denedi.");
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Profil düzenleme formu hatalı gönderildi.");
                return View(model);
            }

            var userId = GetUserId();
            var result = await _profileService.UpdateProfileEditAsync(userId, model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    _logger.LogWarning("Profil düzenleme hatası: {Error}", error.Description);
                }
                return View(model);
            }

            await _profileService.UpdateDefaultAddressAsync(userId, model.Address, model.City, model.District);

            _logger.LogInformation("Profil düzenleme tamamlandı. UserId: {UserId}", userId);
            TempData["ProfileSuccess"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction("Index");
        }
    }
}
