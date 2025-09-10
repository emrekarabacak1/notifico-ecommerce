using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifico.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Notifico.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
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

            var categories = await _productService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.Search = search;

            var products = await _productService.GetAllProductsAsync(search, category);
            var favoriteIds = await _productService.GetFavoriteProductIdsAsync(userId);
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

            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product/Details: User (ID: {UserId}) tried to access non-existent product (ProductId: {ProductId})", userId, id);
                return NotFound();
            }

            var isFavorite = await _productService.IsProductFavoriteAsync(userId, id);
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

            await _productService.ToggleFavoriteAsync(userId, id);

            var isFavorite = await _productService.IsProductFavoriteAsync(userId, id);
            TempData["Success"] = isFavorite ? "Ürün favorilere eklendi!" : "Ürün favorilerden kaldırıldı!";

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize]
        public async Task<IActionResult> MyFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favorites = await _productService.GetUserFavoritesAsync(userId);
            return View("FavoriteList", favorites);
        }
    }
}
