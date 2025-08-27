using Microsoft.AspNetCore.Mvc;
using Notifico.Data;
using Notifico.Models;
using System.Security.Cryptography;
using System.Text;

namespace Notifico.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string salt = GenerateSalt();
            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                PasswordHash = HashPassword(model.Password, salt),
                Salt = salt,
                Role = "User",
                DateCreated = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login", "Account");
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
                _httpContextAccessor.HttpContext!.Session.SetString("UserName", user.UserName);
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
            _httpContextAccessor.HttpContext!.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var combined = Encoding.UTF8.GetBytes(password + salt);
                var hash = sha256.ComputeHash(combined);
                return Convert.ToBase64String(hash);
            }
        }

        public static string GenerateSalt(int size = 32)
        {
            // RNGCryptoServiceProvider
            var buff = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buff);
            }
            return Convert.ToBase64String(buff);
        }
    }
}
