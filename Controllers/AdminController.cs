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
            var userName = TempData["UserName"] as string;
            if(userName == null)
            {
                return RedirectToAction("Login","Account");
            }

            var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
            if (user.Role == "Admin" && user!=null)
            {
                return View();
            }

            return RedirectToAction("Index","Home");
        }
    }
}
