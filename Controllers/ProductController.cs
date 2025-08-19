using Microsoft.AspNetCore.Mvc;
using Notifico.Data;
using Notifico.Models;
using System.Diagnostics;

namespace Notifico.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if(TempData["UserName"] != null)
            {
                var products = _context.Products.ToList();
                return View(products);
            }
            return RedirectToAction("Login","Account");
        }
    }
}
