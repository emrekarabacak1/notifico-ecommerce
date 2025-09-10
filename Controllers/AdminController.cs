using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Notifico.Models;
using Notifico.Services;
using Notifico.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using System.Globalization;

namespace Notifico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminService adminService,
            UserManager<AppUser> userManager,
            ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _userManager = userManager;
            _logger = logger;

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Admin Paneli";
            ViewBag.ProductCount = (await _adminService.GetAllProductsAsync()).Count;
            ViewBag.OrderCount = (await _adminService.GetAllOrdersAsync(null, null)).Count;
            ViewBag.TotalSales = (await _adminService.GetAllOrdersAsync(null, null)).Sum(o => o.TotalAmount);

            return View();
        }

        public async Task<IActionResult> ProductList()
        {
            var products = await _adminService.GetAllProductsAsync();
            _logger.LogInformation("Admin ürün listesi görüntülendi.");
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _adminService.GetProductByIdAsync(id);
            if (product == null)
                return RedirectToAction("ProductList");

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

            var product = await _adminService.GetProductByIdAsync(model.Id);
            if (product == null)
                return RedirectToAction("ProductList");

            var allProducts = await _adminService.GetAllProductsAsync();
            if (allProducts.Any(p => p.Id != model.Id && p.Name.ToLower() == model.Name.ToLower()))
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

            await _adminService.UpdateProductAsync(product);

            TempData["StatusMessage"] = "Ürün güncellendi.";
            return RedirectToAction("ProductList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _adminService.DeleteProductAsync(id);
            return RedirectToAction("ProductList");
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            return View("ProductForm", new ProductFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ProductForm", model);

            var allProducts = await _adminService.GetAllProductsAsync();
            if (allProducts.Any(p => p.Name.ToLower() == model.Name.ToLower()))
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

            await _adminService.AddProductAsync(product);

            TempData["StatusMessage"] = "Ürün eklendi.";
            return RedirectToAction("ProductList");
        }

        public async Task<IActionResult> OrderList(string search, OrderStatus? status)
        {
            var orders = await _adminService.GetAllOrdersAsync(search, status);
            return View(orders);
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _adminService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadOrderPdf(int id)
        {
            var order = await _adminService.GetOrderByIdAsync(id);
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
                        row.RelativeColumn().Text("SİPARİŞ FATURASI")
                           .FontSize(22).Bold().FontColor(Colors.Blue.Medium);
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
                            row.ConstantColumn(120).AlignRight()
                               .Text(GetOrderStatusText(order.Status))
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

                        col.Item().PaddingTop(10).AlignRight()
                           .Text($"Toplam: {order.TotalAmount:C}")
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
            if (!Enum.IsDefined(typeof(OrderStatus), status))
                return RedirectToAction("OrderDetail", new { id });

            await _adminService.UpdateOrderStatusAsync(id, (OrderStatus)status);
            return RedirectToAction("OrderDetail", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            await _adminService.CancelOrderAsync(id);
            return RedirectToAction("OrderList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            await _adminService.DeleteOrderAsync(id);
            return RedirectToAction("OrderList");
        }

        public async Task<IActionResult> Dashboard()
        {
            var dashboardView = await _adminService.GetDashboardViewModelAsync();
            dashboardView.TotalCustomerCount = (await _userManager.GetUsersInRoleAsync("User")).Count;

            return View(dashboardView);
        }

        [HttpGet]
        public async Task<IActionResult> LowStockProducts()
        {
            var products = await _adminService.GetLowStockProductsAsync(10);
            return PartialView("_LowStockProductsPartial", products);
        }
    }
}
