using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifico.Data;
using Notifico.Models;
using System.Globalization;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Admin Paneli";

        ViewBag.ProductCount = await _context.Products.CountAsync();
        ViewBag.OrderCount = await _context.Orders.CountAsync();
        ViewBag.TotalSales = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        return View();
    }

    public async Task<IActionResult> ProductList()
    {
        var products = await _context.Products.ToListAsync();
        _logger.LogInformation("Admin ürün listesi görüntülendi.");
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return RedirectToAction("ProductList");
        return View("ProductForm", product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Product product)
    {
        if (!ModelState.IsValid) return View(product);

        var productInDb = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
        if (productInDb == null) return RedirectToAction("ProductList");

        if (await _context.Products.AnyAsync(p => p.Id != product.Id && p.Name.ToLower() == product.Name.ToLower()))
        {
            ModelState.AddModelError("Name", "Bu isimde zaten başka bir ürün var.");
            return View(product);
        }

        productInDb.Name = product.Name;
        productInDb.Description = product.Description;
        productInDb.Price = product.Price;
        productInDb.Stock = product.Stock;
        productInDb.Category = product.Category;
        productInDb.ImageUrl = product.ImageUrl;

        await _context.SaveChangesAsync();
        return RedirectToAction("ProductList");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("ProductList");
    }

    [HttpGet]
    public IActionResult AddProduct() => View("ProductForm", new Product());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(Product product)
    {
        if (!ModelState.IsValid) return View(product);

        if (await _context.Products.AnyAsync(p => p.Name.ToLower() == product.Name.ToLower()))
        {
            ModelState.AddModelError("Name", "Bu isimde zaten bir ürün var.");
            return View(product);
        }

        product.DateAdded = DateTime.UtcNow;
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        return RedirectToAction("ProductList");
    }

    public async Task<IActionResult> OrderList(string search, OrderStatus? status)
    {
        var orders = _context.Orders.Include(o => o.User).OrderByDescending(o => o.OrderDate).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            orders = orders.Where(o => o.User.UserName.ToLower().Contains(search.ToLower()));

        if (status.HasValue)
            orders = orders.Where(o => o.Status == status.Value);

        return View(await orders.ToListAsync());
    }

    public async Task<IActionResult> OrderDetail(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, int status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        if (!Enum.IsDefined(typeof(OrderStatus), status)) return RedirectToAction("OrderDetail", new { id });

        order.Status = (OrderStatus)status;
        await _context.SaveChangesAsync();
        return RedirectToAction("OrderDetail", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return RedirectToAction("OrderList");

        if (order.Status == OrderStatus.IptalEdildi || order.Status == OrderStatus.TeslimEdildi)
            return RedirectToAction("OrderList");

        order.Status = OrderStatus.IptalEdildi;
        await _context.SaveChangesAsync();
        return RedirectToAction("OrderList");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
        if (order != null)
        {
            if (order.OrderItems != null && order.OrderItems.Any())
                _context.OrderItems.RemoveRange(order.OrderItems);

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("OrderList");
    }

    public async Task<IActionResult> Dashboard()
    {
        var totalOrderCount = await _context.Orders.CountAsync();
        var totalSales = await _context.Orders.SumAsync(o => o.TotalAmount);
        var totalCustomerCount = (await _userManager.GetUsersInRoleAsync("User")).Count;

        var recentOrders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .ToListAsync();

        var chartLabels = new List<string>();
        var chartData = new List<decimal>();

        for (int i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
            monthStart = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            monthEnd = DateTime.SpecifyKind(monthEnd, DateTimeKind.Utc);

            chartLabels.Add(monthStart.ToString("MMMM yyyy", new CultureInfo("tr-TR")));

            decimal total = await _context.Orders
                .Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            chartData.Add(total);
        }

        var dashboardView = new ViewModel
        {
            TotalOrderCount = totalOrderCount,
            TotalSales = totalSales,
            TotalCustomerCount = totalCustomerCount,
            RecentOrders = recentOrders ?? new List<Order>(),
            ChartLabels = chartLabels ?? new List<string>(),
            ChartData = chartData ?? new List<decimal>()
        };

        return View(dashboardView);
    }
}
