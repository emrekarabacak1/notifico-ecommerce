using Microsoft.AspNetCore.Mvc;
using Notifico.Data;
using Notifico.Helpers;
using Notifico.Models;
using System.Diagnostics;

namespace Notifico.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Index()
        {
            var user = _httpContextAccessor.GetCurrentUser(_context);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Role = user.Role;
            var products = _context.Products.ToList();
            return View(products);
        }
    }
}
