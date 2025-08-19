using Microsoft.AspNetCore.Mvc;
using Notifico.Data;
using Notifico.Models;
using System;
using System.Security.Cryptography;
using System.Text;
namespace Notifico.Controllers
{
    public class AccountController : Controller
    {

        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string UserName, string Email, string Password)
        {
            string passwordHash = HashPassword(Password);

            var user = new User
            {
                UserName = UserName,
                Email = Email,
                PasswordHash = passwordHash,
                Role = "User",
                DateCreated = DateTime.Now,
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login","Account");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login()
        {
            return View();
        }


        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);

                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString(); // <--- return burada!
            }
        }

    }


}
