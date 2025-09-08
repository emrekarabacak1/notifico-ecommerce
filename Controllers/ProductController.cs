using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notifico.Data;
using Notifico.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Notifico.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string search, string category)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("Product/Index: Unauthorized user tried to access product list. IP: {Ip}", HttpContext.Connection.RemoteIpAddress?.ToString());
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("Admin"))
            {
                _logger.LogWarning("Product/Index: Admin user (ID: {UserId}) tried to access product list. Access denied.", userId);
                return Forbid();
            }

            _logger.LogInformation("Product/Index: User (ID: {UserId}) viewed product list.", userId);

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.ToLower() == category.ToLower());

            var categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.Search = search;

            var products = await query.ToListAsync();

            var favoriteIds = await _context.FavoriteProducts
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToListAsync();
            ViewBag.FavoriteIds = favoriteIds;

            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Admin"))
            {
                _logger.LogWarning("Product/Details: Admin user (ID: {UserId}) tried to access product details (ProductId: {ProductId}). Access denied.", userId, id);
                return Forbid();
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                _logger.LogWarning("Product/Details: User (ID: {UserId}) tried to access non-existent product (ProductId: {ProductId})", userId, id);
                return NotFound();
            }

            var isFavorite = await _context.FavoriteProducts
                .AnyAsync(f => f.UserId == userId && f.ProductId == id);
            ViewBag.IsFavorite = isFavorite;

            _logger.LogInformation("Product/Details: User (ID: {UserId}) viewed product (ProductId: {ProductId})", userId, id);

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var existing = await _context.FavoriteProducts
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == id);

            if (existing == null)
            {
                _context.FavoriteProducts.Add(new FavoriteProduct
                {
                    UserId = userId,
                    ProductId = id,
                    DateAdded = DateTime.UtcNow
                });
                TempData["Success"] = "Ürün favorilere eklendi!";
            }
            else
            {
                _context.FavoriteProducts.Remove(existing);
                TempData["Info"] = "Ürün favorilerden kaldırıldı!";
            }
            await _context.SaveChangesAsync();
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize]
        public async Task<IActionResult> MyFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favorites = await _context.FavoriteProducts
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                .OrderByDescending(f => f.DateAdded)
                .ToListAsync();

            return View("FavoriteList", favorites);
        }
    }
}
