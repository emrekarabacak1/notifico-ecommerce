using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    
    public IActionResult OrderList(string search, OrderStatus? status)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if(string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        var orders = _context.Orders.Include(o => o.User).OrderByDescending(o => o.OrderDate).AsQueryable();
        if(!string.IsNullOrEmpty(search))
        {
            orders = orders.Where(o=> o.User.UserName.ToLower().Contains(search.ToLower()));
        }

        if(status.HasValue)
        {
            orders = orders.Where(o=>o.Status== status.Value);
        }

        var filteredOrders = orders.ToList();
        

        return View(filteredOrders);
    }

    public IActionResult OrderDetail(int id)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if(string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(o => o.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        var order = _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).Include(u=>u.User).FirstOrDefault(p=>p.Id == id);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateOrderStatus(int id, int status)
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

        var order = _context.Orders.FirstOrDefault(o => o.Id == id);
        if (order == null)
        {
            return NotFound();
        }

        if (!Enum.IsDefined(typeof(OrderStatus), status))
        {
            TempData["StatusMessage"] = "Geçersiz statü!";
            return RedirectToAction("OrderDetail", new { id = order.Id });
        }

        order.Status = (OrderStatus)status;
        _context.SaveChanges();

        TempData["StatusMessage"] = "Sipariş statüsü güncellendi.";
        return RedirectToAction("OrderDetail", new { id = order.Id });
    }


}
