using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notifico.Data;
using Notifico.Models;
using System.Security.Claims;

namespace Notifico.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var products = await _context.Products.ToListAsync();
            return View(products);
        }
    }
}
