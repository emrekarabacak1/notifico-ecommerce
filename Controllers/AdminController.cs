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
        if (product == null)
        {
            _logger.LogWarning("Admin: Düzenlenmek istenen ürün bulunamadı. ProductId: {ProductId}", id);
            return RedirectToAction("ProductList");
        }
        _logger.LogInformation("Admin ürün düzenleme sayfasını açtı. ProductId: {ProductId}", id);
        return View("ProductForm", product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Product product)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Admin: Ürün düzenleme formu hatalı gönderildi.");
            return View(product);
        }

        var productInDb = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
        if (productInDb == null)
        {
            _logger.LogWarning("Admin: Düzenlenmek istenen ürün bulunamadı. ProductId: {ProductId}", product.Id);
            return RedirectToAction("ProductList");
        }

        if (await _context.Products.AnyAsync(p => p.Id != product.Id && p.Name.ToLower() == product.Name.ToLower()))
        {
            ModelState.AddModelError("Name", "Bu isimde zaten başka bir ürün var.");
            _logger.LogWarning("Admin: Aynı isimde başka bir ürün var. ProductName: {ProductName}", product.Name);
            return View(product);
        }

        productInDb.Name = product.Name;
        productInDb.Description = product.Description;
        productInDb.Price = product.Price;
        productInDb.Stock = product.Stock;
        productInDb.Category = product.Category;
        productInDb.ImageUrl = product.ImageUrl;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin ürün güncelledi. ProductId: {ProductId}", product.Id);
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
            _logger.LogInformation("Admin ürün sildi. ProductId: {ProductId}", id);
        }
        else
        {
            _logger.LogWarning("Admin: Silinmek istenen ürün bulunamadı. ProductId: {ProductId}", id);
        }
        return RedirectToAction("ProductList");
    }

    [HttpGet]
    public IActionResult AddProduct()
    {
        _logger.LogInformation("Admin yeni ürün ekleme sayfasını açtı.");
        return View("ProductForm", new Product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(Product product)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Admin: Ürün ekleme formu hatalı gönderildi.");
            return View(product);
        }

        if (await _context.Products.AnyAsync(p => p.Name.ToLower() == product.Name.ToLower()))
        {
            ModelState.AddModelError("Name", "Bu isimde zaten bir ürün var.");
            _logger.LogWarning("Admin: Aynı isimde başka bir ürün eklemeye çalıştı. ProductName: {ProductName}", product.Name);
            return View(product);
        }

        product.DateAdded = DateTime.UtcNow;
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin yeni ürün ekledi. ProductId: {ProductId}, Name: {Name}", product.Id, product.Name);

        return RedirectToAction("ProductList");
    }

    public async Task<IActionResult> OrderList(string search, OrderStatus? status)
    {
        var orders = _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            orders = orders.Where(o => o.User.UserName.ToLower().Contains(search.ToLower()));
            _logger.LogInformation("Admin sipariş listesi filtreli arandı. Search: {Search}", search);
        }

        if (status.HasValue)
        {
            orders = orders.Where(o => o.Status == status.Value);
            _logger.LogInformation("Admin sipariş listesi statü ile filtrelendi. Status: {Status}", status.Value);
        }

        _logger.LogInformation("Admin sipariş listesi görüntülendi.");
        return View(await orders.ToListAsync());
    }

    public async Task<IActionResult> OrderDetail(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            _logger.LogWarning("Admin: Sipariş detayı bulunamadı. OrderId: {OrderId}", id);
            return NotFound();
        }
        _logger.LogInformation("Admin sipariş detay sayfasını görüntüledi. OrderId: {OrderId}", id);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, int status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Admin: Statüsü güncellenmek istenen sipariş bulunamadı. OrderId: {OrderId}", id);
            return NotFound();
        }

        if (!Enum.IsDefined(typeof(OrderStatus), status))
        {
            TempData["StatusMessage"] = "Geçersiz statü!";
            _logger.LogWarning("Admin: Sipariş için geçersiz statü gönderildi. OrderId: {OrderId}, Status: {Status}", id, status);
            return RedirectToAction("OrderDetail", new { id });
        }

        order.Status = (OrderStatus)status;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin sipariş statüsünü güncelledi. OrderId: {OrderId}, Status: {Status}", id, order.Status);

        TempData["StatusMessage"] = "Sipariş statüsü güncellendi.";
        return RedirectToAction("OrderDetail", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            TempData["StatusMessage"] = "Sipariş Bulunamadı";
            _logger.LogWarning("Admin: İptal edilmek istenen sipariş bulunamadı. OrderId: {OrderId}", id);
            return RedirectToAction("OrderList");
        }

        if (order.Status == OrderStatus.IptalEdildi || order.Status == OrderStatus.TeslimEdildi)
        {
            TempData["StatusMessage"] = "Bu sipariş iptal veya teslim edilmiş.";
            _logger.LogInformation("Admin: Sipariş zaten iptal edilmiş ya da teslim edilmiş. OrderId: {OrderId}, Status: {Status}", id, order.Status);
            return RedirectToAction("OrderList");
        }

        order.Status = OrderStatus.IptalEdildi;
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Sipariş başarıyla iptal edildi.";
        _logger.LogInformation("Admin siparişi iptal etti. OrderId: {OrderId}", id);
        return RedirectToAction("OrderList");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            TempData["StatusMessage"] = "Sipariş bulunamadı!";
            _logger.LogWarning("Admin: Silinmek istenen sipariş bulunamadı. OrderId: {OrderId}", id);
            return RedirectToAction("OrderList");
        }

        if (order.OrderItems != null && order.OrderItems.Any())
        {
            _context.OrderItems.RemoveRange(order.OrderItems);
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Sipariş başarıyla silindi.";
        _logger.LogInformation("Admin sipariş sildi. OrderId: {OrderId}", id);
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
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
                                .AddMonths(-i);
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

        _logger.LogInformation("Admin dashboard görüntülendi.");
        return View(dashboardView);
    }
}
