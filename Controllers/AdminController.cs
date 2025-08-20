using Microsoft.AspNetCore.Mvc;
using Notifico.Data;
using Notifico.Models;


public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult ProductList()
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }
            

        var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }
            
        var products = _context.Products.ToList();
        return View(products); 
    }

    [HttpGet]
    public IActionResult EditProduct(int id)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        var product = _context.Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return RedirectToAction("ProductList");
        }

        return View(product);
    }

    [HttpPost]
    public IActionResult EditProduct(Product product)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }
            

        var productInDb = _context.Products.FirstOrDefault(p => p.Id == product.Id);
        if (productInDb == null)
        {
            return RedirectToAction("ProductList");
        }

        productInDb.Name = product.Name;
        productInDb.Description = product.Description;
        productInDb.Price = product.Price;
        productInDb.Stock = product.Stock;
        productInDb.Category = product.Category;
        productInDb.ImageUrl = product.ImageUrl;

        _context.SaveChanges();

        return RedirectToAction("ProductList");
    }

    public IActionResult DeleteProduct(int id)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        var product = _context.Products.FirstOrDefault(p => p.Id == id);
        if (product != null)
        {
            _context.Products.Remove(product);
            _context.SaveChanges();
        }
        return RedirectToAction("ProductList");
    }

    [HttpGet]
    public IActionResult AddProduct()
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    public IActionResult AddProduct(Product product)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        product.DateAdded = DateTime.UtcNow;
        _context.Products.Add(product);
        _context.SaveChanges();

        return RedirectToAction("ProductList");
    }


}
