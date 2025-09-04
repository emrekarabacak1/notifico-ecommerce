using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notifico.Data;
using System.Security.Claims;
using Notifico.Models;

namespace Notifico.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AddressController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        public async Task<IActionResult> Index()
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

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
            ModelState.Remove("User"); // 

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            address.UserId = userId;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                ViewBag.ModelStateErrors = errors.Select(e => e.ErrorMessage).ToList();
                return View(address);
            }

            if (address.IsDefault)
            {
                var existingDefaults = await _context.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                foreach (var item in existingDefaults)
                    item.IsDefault = false;
            }

            _context.Add(address);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Adres başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addressDb = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == address.Id && a.UserId == userId);
            if (addressDb == null)
            {
                TempData["Error"] = "Adres bulunamadı veya erişiminiz yok.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                ViewBag.ModelStateErrors = errors.Select(e => e.ErrorMessage).ToList();
                return View(address);
            }

            addressDb.Title = address.Title;
            addressDb.FullAddress = address.FullAddress;
            addressDb.City = address.City;
            addressDb.District = address.District;
            addressDb.ZipCode = address.ZipCode;

            if (address.IsDefault)
            {
                var existingDefaults = await _context.Addresses.Where(a => a.UserId == userId && a.IsDefault && a.Id != address.Id).ToListAsync();
                foreach (var item in existingDefaults)
                    item.IsDefault = false;
            }
            addressDb.IsDefault = address.IsDefault;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Adres güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null)
                return NotFound();

            bool usedInOrders = await _context.Orders.AnyAsync(o => o.AddressId == address.Id);

            if (usedInOrders)
            {
                TempData["Error"] = "Bu adres geçmişte bir siparişte kullanıldığı için silinemez.";
                return RedirectToAction(nameof(Index));
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Adres başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var addresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
            bool found = false;
            foreach (var address in addresses)
            {
                if (address.Id == id)
                {
                    address.IsDefault = true;
                    found = true;
                }
                else
                {
                    address.IsDefault = false;
                }
            }

            if (!found)
            {
                TempData["Error"] = "Adres bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Varsayılan adres ayarlandı.";
            return RedirectToAction(nameof(Index));
        }
    }
}
