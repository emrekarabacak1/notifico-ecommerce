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
        public IActionResult Register(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            string salt = GenerateSalt();
            user.PasswordHash = HashPassword(user.PasswordHash, salt);
            user.Salt = salt;
            user.Role = "User";
            user.DateCreated = DateTime.UtcNow;

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
        public IActionResult Login(string Email, string Password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == Email);
            if (user == null)
            {
                ViewBag.Error = "Email veya Şifre Hatalı";
                return View();
            }

            string hashed = HashPassword(Password, user.Salt);

            if (user.PasswordHash == hashed)
            {
                HttpContext.Session.SetString("UserName", user.UserName);
                return RedirectToAction("Index", "Product");
            }
            else
            {
                ViewBag.Error = "Email veya Şifre Hatalı";
                return View();
            }
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");

        }


        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var combined = Encoding.UTF8.GetBytes(password + salt);
                var hash = sha256.ComputeHash(combined);
                return Convert.ToBase64String(hash);
            }
        }

        public static string GenerateSalt(int size = 32)
        {
            var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var buff = new byte[size];
            rng.GetBytes(buff);
            return Convert.ToBase64String(buff);
        }

    }


}
