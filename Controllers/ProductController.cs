using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notifico.Data;
using Notifico.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Notifico.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
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

            var products = await _context.Products.ToListAsync();
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

            _logger.LogInformation("Product/Details: User (ID: {UserId}) viewed product (ProductId: {ProductId})", userId, id);

            return View(product);
        }
    }
}
