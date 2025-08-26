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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelOrder(int id)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(u=>u.UserName==userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        var order = _context.Orders.FirstOrDefault(o => o.Id == id);
        if(order == null)
        {
            TempData["StatusMessage"] = "Sipariş Bulunamadı"; 
            return RedirectToAction("OrderList");
        }

        if (order.Status == OrderStatus.IptalEdildi || order.Status == OrderStatus.TeslimEdildi)
        {
            TempData["StatusMessage"] = "Bu sipariş iptal veya teslim edilmiş.";
            return RedirectToAction("OrderList");
        }
        order.Status = OrderStatus.IptalEdildi;
        _context.SaveChanges();
        TempData["StatusMessage"] = "Sipariş başarıyla iptal edildi.";
        return RedirectToAction("OrderList");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteOrder(int id)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if(string.IsNullOrEmpty(userName)) 
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
        if (user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        var order = _context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id);
        if (order == null)
        {
            TempData["StatusMessage"] = "Sipariş bulunamadı!";
            return RedirectToAction("OrderList");
        }

        if (order.OrderItems != null && order.OrderItems.Any())
        {
            _context.OrderItems.RemoveRange(order.OrderItems);
        }

        _context.Orders.Remove(order);
        _context.SaveChanges();

        TempData["StatusMessage"] = "Sipariş başarıyla silindi.";
        return RedirectToAction("OrderList");


    }

    public IActionResult Dashboard()
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrEmpty(userName))
        {
            return RedirectToAction("Login","Account");
        }

        var user = _context.Users.FirstOrDefault(o => o.UserName == userName);
        if(user == null || user.Role != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        var totalOrderCount = _context.Orders.Count();
        var totalSales = _context.Orders.Sum(o=>o.TotalAmount);
        var totalCustomerCount = _context.Users.Count(u=>u.Role=="User");

        var recentOrders = _context.Orders.Include(o => o.User).OrderByDescending(o => o.OrderDate).Take(5).ToList();
        
        var chartLabels = new List<string>();
        var chartData = new List<decimal>();


        for (int i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
                                .AddMonths(-i);
            monthStart = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);

            var monthEnd = monthStart.AddMonths(1);
            monthEnd = DateTime.SpecifyKind(monthEnd, DateTimeKind.Utc);

            string label = monthStart.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
            chartLabels.Add(label);

            decimal total = _context.Orders.Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd).Sum(o => (decimal?)o.TotalAmount) ?? 0;
            chartData.Add(total);
        }


        var dashboardView = new ViewModel
        {
            TotalOrderCount = totalOrderCount,
            TotalSales = totalSales,
            TotalCustomerCount = totalCustomerCount,
            RecentOrders = recentOrders,
            ChartLabels = chartLabels,
            ChartData = chartData
        };

        return View(dashboardView);
    }

}
