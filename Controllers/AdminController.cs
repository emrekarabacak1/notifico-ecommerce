using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifico.Data;
using Notifico.Models;
using Notifico.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
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

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
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

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            Category = product.Category,
            ImageUrl = product.ImageUrl
        };
        return View("ProductForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("ProductForm", model);

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (product == null) return RedirectToAction("ProductList");

        if (await _context.Products.AnyAsync(p => p.Id != model.Id && p.Name.ToLower() == model.Name.ToLower()))
        {
            ModelState.AddModelError("Name", "Bu isimde zaten başka bir ürün var.");
            return View("ProductForm", model);
        }

        string? imageUrl = product.ImageUrl;

        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/products");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(model.ImageFile.FileName);
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(stream);
            }

            imageUrl = "/uploads/products/" + fileName;
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            imageUrl = model.ImageUrl;
        }

        product.Name = model.Name;
        product.Description = model.Description;
        product.Price = model.Price;
        product.Stock = model.Stock;
        product.Category = model.Category;
        product.ImageUrl = imageUrl;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Ürün güncellendi.";
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
    public async Task<IActionResult> AddProduct()
    {
        return View("ProductForm", new ProductFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("ProductForm", model);

        if (await _context.Products.AnyAsync(p => p.Name.ToLower() == model.Name.ToLower()))
        {
            ModelState.AddModelError("Name", "Bu isimde zaten bir ürün var.");
            return View("ProductForm", model);
        }

        string? imageUrl = null;

        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/products");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(model.ImageFile.FileName);
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(stream);
            }

            imageUrl = "/uploads/products/" + fileName;
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            imageUrl = model.ImageUrl;
        }

        var product = new Product
        {
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            Stock = model.Stock,
            Category = model.Category,
            ImageUrl = imageUrl,
            DateAdded = DateTime.UtcNow
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Ürün eklendi.";
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

    [HttpGet]
    public async Task<IActionResult> DownloadOrderPdf(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        string address = order.AddressSnapshot ?? order.Address?.FullAddress ?? "Adres bulunamadı";
        string userName = order.User?.UserName ?? "Müşteri";

        var fileName = $"Siparis_{order.Id}_{order.OrderDate:yyyyMMddHHmm}.pdf";
        var pdfBytes = GenerateOrderPdf(order, address, userName);

        return File(pdfBytes, "application/pdf", fileName);
    }

    private byte[] GenerateOrderPdf(Order order, string address, string userName)
    {
        using var stream = new MemoryStream();
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header().Row(row =>
                {
                    row.RelativeColumn().Text("SİPARİŞ FATURASI").FontSize(22).Bold().FontColor(Colors.Blue.Medium);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeColumn().Text($"Sipariş No: {order.Id}").Bold();
                        row.RelativeColumn().AlignRight().Text($"Tarih: {order.OrderDate:dd.MM.yyyy HH:mm}");
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeColumn().Text($"Müşteri: {userName}");
                        row.RelativeColumn().AlignRight().Text($"Durum:").Bold();
                        row.ConstantColumn(120).AlignRight().Text(GetOrderStatusText(order.Status))
                            .FontColor(GetOrderStatusColor(order.Status))
                            .SemiBold();
                    });

                    col.Item().Text($"Adres: {address}").FontSize(10);

                    col.Item().PaddingTop(10).Text("Ürünler:").FontSize(14).Bold();

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4); 
                            columns.RelativeColumn(2); 
                            columns.RelativeColumn(2); 
                            columns.RelativeColumn(2); 
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Text("Ürün").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Text("Adet").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Text("Birim Fiyat").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Text("Tutar").Bold();
                        });

                        foreach (var item in order.OrderItems)
                        {
                            table.Cell().Text(item.Product?.Name ?? "-");
                            table.Cell().Text(item.Quantity.ToString());
                            table.Cell().Text($"{item.UnitPrice:C}");
                            table.Cell().Text($"{(item.Quantity * item.UnitPrice):C}");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Text($"Toplam: {order.TotalAmount:C}")
                        .FontSize(15).Bold().FontColor(Colors.Black);

                });

                page.Footer().AlignCenter().Text("Teşekkür ederiz!").FontSize(11).Italic();
            });
        });

        doc.GeneratePdf(stream);
        return stream.ToArray();

        string GetOrderStatusText(OrderStatus status) => status switch
        {
            OrderStatus.Beklemede => "Beklemede",
            OrderStatus.Onaylandı => "Onaylandı",
            OrderStatus.KargoyaVerildi => "Kargoya Verildi",
            OrderStatus.TeslimEdildi => "Teslim Edildi",
            OrderStatus.IptalEdildi => "İptal Edildi",
            _ => status.ToString()
        };

        string GetOrderStatusColor(OrderStatus status) => status switch
        {
            OrderStatus.Beklemede => Colors.Grey.Darken2,
            OrderStatus.Onaylandı => Colors.Green.Darken2,
            OrderStatus.KargoyaVerildi => Colors.Orange.Medium,
            OrderStatus.TeslimEdildi => Colors.Blue.Darken1,
            OrderStatus.IptalEdildi => Colors.Red.Accent2,
            _ => Colors.Black
        };
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

        var topProducts = await _context.OrderItems
            .Include(oi => oi.Product)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
            .Select(g => new ProductSalesInfo
            {
                ProductName = g.Key.Name,
                TotalSold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(5)
            .ToListAsync();

        var categorySales = await _context.OrderItems
            .Include(oi => oi.Product)
            .GroupBy(oi => oi.Product.Category)
            .Select(g => new CategorySalesInfo
            {
                CategoryName = g.Key,
                TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.TotalRevenue)
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
            ChartData = chartData ?? new List<decimal>(),
            TopProducts = topProducts ?? new List<ProductSalesInfo>(),
            CategorySales = categorySales ?? new List<CategorySalesInfo>()
        };

        return View(dashboardView);
    }

    [HttpGet]
    public async Task<IActionResult> LowStockProducts()
    {
        var products = await _context.Products
            .Where(p => p.Stock <= 10)
            .OrderBy(p => p.Stock)
            .ToListAsync();

        return PartialView("_LowStockProductsPartial", products);
    }
}
