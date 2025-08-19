using Microsoft.AspNetCore.Mvc;
using Notifico.Data;
using Notifico.Models;

namespace Notifico.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public ActionResult Index()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
            if (user != null && user.Role == "Admin")
            {
                return View();
            }
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ProductList()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if(string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login","Account");
            }

            var user = _context.Users.FirstOrDefault(x => x.UserName == userName);

            if(user != null && user.Role == "Admin") 
            {
                var product = _context.Products.ToList();
                return View(product);
            }
            return RedirectToAction("Index","Home");
        }
    }
}
