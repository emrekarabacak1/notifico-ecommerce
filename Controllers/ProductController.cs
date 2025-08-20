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
            var userName = HttpContext.Session.GetString("UserName");
            if (!string.IsNullOrEmpty(userName))
            {
                var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
                if (user != null)
                {
                    ViewBag.Role = user.Role;
                    var products = _context.Products.ToList();
                    return View(products);
                }
            }
            return RedirectToAction("Login", "Account");
        }
    }
}
