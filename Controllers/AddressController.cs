using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifico.Models;
using Notifico.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Notifico.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly IAddressService _addressService;
        private readonly ILogger<AddressController> _logger;

        public AddressController(IAddressService addressService, ILogger<AddressController> logger)
        {
            _addressService = addressService;
            _logger = logger;
        }

        private bool IsAdmin() => User.IsInRole("Admin");
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index()
        {
            if (IsAdmin()) return Forbid();

            var addresses = await _addressService.GetUserAddressesAsync(UserId);

            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];
            return View(addresses);
        }

        [HttpGet]
        public IActionResult Add()
        {
            if (IsAdmin()) return Forbid();
            return View(new Address());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Address address)
        {
            if (IsAdmin()) return Forbid();

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            address.UserId = UserId;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ViewBag.ModelStateErrors = errors;
                return View(address);
            }

            await _addressService.AddAddressAsync(address);
            TempData["Success"] = "Adres başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (IsAdmin()) return Forbid();

            var address = await _addressService.GetByIdAsync(id, UserId);
            if (address == null)
            {
                TempData["Error"] = "Adres bulunamadı veya erişiminiz yok.";
                return RedirectToAction(nameof(Index));
            }
            return View(address);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Address address)
        {
            if (IsAdmin()) return Forbid();

            ModelState.Remove("UserId");
            ModelState.Remove("User");

            var result = await _addressService.UpdateAddressAsync(address, UserId);
            if (!result)
            {
                TempData["Error"] = "Adres güncellenemedi veya bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Adres güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (IsAdmin()) return Forbid();

            var result = await _addressService.DeleteAddressAsync(id, UserId);
            if (!result)
            {
                TempData["Error"] = "Adres silinemedi. Siparişte kullanılmış olabilir veya bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Adres başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            if (IsAdmin()) return Forbid();

            var result = await _addressService.SetDefaultAddressAsync(id, UserId);
            if (!result)
            {
                TempData["Error"] = "Adres bulunamadı veya default yapılamadı.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Varsayılan adres ayarlandı ve profil adresiniz de güncellendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
