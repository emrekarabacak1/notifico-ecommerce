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

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addresses = await _context.Addresses.Where(a => a.UserId == userId).OrderByDescending(a => a.IsDefault).ToListAsync();

            return View(addresses);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Address address)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            address.UserId = userId;

            if (address.IsDefault)
            {
                var existingDefaults = _context.Addresses.Where(a => a.UserId == userId && a.IsDefault);
                foreach (var item in existingDefaults)
                {
                    item.IsDefault = false;
                }
            }

            _context.Add(address);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            return View(address);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Address address)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            address.UserId = userId;

            if (address.UserId != userId)
            {
                return Unauthorized();
            }

            if (address.IsDefault)
            {
                var existingDefaults = _context.Addresses.Where(a => a.UserId == userId && a.IsDefault && a.Id != address.Id);
                foreach (var item in existingDefaults)
                {
                    item.IsDefault = false;
                }
            }

            _context.Update(address);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null)
            {
                return NotFound();
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        [HttpPost]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var addresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
            foreach (var address in addresses)
            {
                address.IsDefault = (address.Id == id);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
