using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; 
using Notifico.Data;
using Notifico.Hubs;
using Notifico.Models;
using Notifico.ViewModels;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace Notifico.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<CartController> _logger; 

        public CartController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, ILogger<CartController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger; 
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                TempData["Error"] = "Ürün Bulunamadı";
                _logger.LogWarning("Sepete ekleme başarısız! User: {UserId}, ProductId: {ProductId} Ürün bulunamadı", userId, productId);
                return RedirectToAction("Index", "Product");
            }

            var cart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem != null)
            {
                if (product.Stock <= cartItem.Quantity)
                {
                    TempData["Error"] = "Sepetteki ürün adedi, mevcut stoktan fazla olamaz!";
                    _logger.LogWarning("Stok yetersiz! User: {UserId}, ProductId: {ProductId} Stok: {Stock}, Sepet: {Quantity}",
                        userId, productId, product.Stock, cartItem.Quantity);
                    return RedirectToAction("Index", "Product");
                }
                cartItem.Quantity++;
                _logger.LogInformation("Sepetteki ürün adedi artırıldı. User: {UserId}, ProductId: {ProductId}, Yeni Adet: {Quantity}", userId, productId, cartItem.Quantity);
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = 1
                };
                await _context.CartItems.AddAsync(cartItem);
                _logger.LogInformation("Ürün sepete eklendi. User: {UserId}, ProductId: {ProductId}", userId, productId);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCartAjax(int productId)
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin kullanıcılar sepete ürün ekleyemez!" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new { success = false, error = "Giriş yapmalısınız" });

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                _logger.LogWarning("AddToCartAjax: Ürün bulunamadı. User: {UserId}, ProductId: {ProductId}", userId, productId);
                return Json(new { success = false, error = "Ürün bulunamadı" });
            }

            var cart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem != null)
            {
                if (product.Stock <= cartItem.Quantity)
                {
                    _logger.LogWarning("AddToCartAjax: Stok yetersiz. User: {UserId}, ProductId: {ProductId}", userId, productId);
                    return Json(new { success = false, error = "Stok yetersiz" });
                }
                cartItem.Quantity++;
                _logger.LogInformation("AddToCartAjax: Sepetteki ürün adedi artırıldı. User: {UserId}, ProductId: {ProductId}, Yeni Adet: {Quantity}", userId, productId, cartItem.Quantity);
            }
            else
            {
                cartItem = new CartItem { CartId = cart.Id, ProductId = productId, Quantity = 1 };
                await _context.CartItems.AddAsync(cartItem);
                _logger.LogInformation("AddToCartAjax: Ürün sepete eklendi. User: {UserId}, ProductId: {ProductId}", userId, productId);
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadOrderPdf(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            string address = order.AddressSnapshot ?? order.Address?.FullAddress ?? "Adres bulunamadı";

            var fileName = $"Siparis_{order.Id}_{order.OrderDate:yyyyMMddHHmm}.pdf";

            var pdfBytes = GenerateOrderPdf(order, address, User.Identity.Name);

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

        [HttpGet]
        public async Task<IActionResult> MyCart()
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cart = await _context.Carts.Include(c => c.CartItems).ThenInclude(ci => ci.Product).FirstOrDefaultAsync(c => c.UserId == userId);
            var cartItems = cart?.CartItems.ToList() ?? new List<CartItem>();

            _logger.LogInformation("Kullanıcı sepetini görüntüledi. User: {UserId}, Ürün Sayısı: {Count}", userId, cartItems.Count);

            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cartItem = await _context.CartItems.Include(ci => ci.Cart).FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Ürün sepetten çıkarıldı. User: {UserId}, ProductId: {ProductId}", userId, cartItem.ProductId);
            }

            return RedirectToAction("MyCart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecreaseQuantity(int id)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cartItem = await _context.CartItems.Include(ci => ci.Cart).Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);

            if (cartItem == null)
                return RedirectToAction("MyCart");

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                _logger.LogInformation("Ürün adedi azaltıldı. User: {UserId}, ProductId: {ProductId}, Yeni Adet: {Quantity}", userId, cartItem.ProductId, cartItem.Quantity);
            }
            else
            {
                _context.CartItems.Remove(cartItem);
                _logger.LogInformation("Sepetten ürün tamamen silindi (adet 1 idi). User: {UserId}, ProductId: {ProductId}", userId, cartItem.ProductId);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyCart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncreaseQuantity(int id)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cartItem = await _context.CartItems.Include(ci => ci.Cart).Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);

            if (cartItem == null)
                return RedirectToAction("MyCart");

            if (cartItem.Product != null && cartItem.Quantity < cartItem.Product.Stock)
            {
                cartItem.Quantity++;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Ürün adedi artırıldı. User: {UserId}, ProductId: {ProductId}, Yeni Adet: {Quantity}", userId, cartItem.ProductId, cartItem.Quantity);
            }
            else
            {
                _logger.LogWarning("Stok yetersiz, ürün adedi artırılamadı. User: {UserId}, ProductId: {ProductId}", userId, cartItem.ProductId);
            }

            return RedirectToAction("MyCart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart?.CartItems.Any() == true)
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Kullanıcı sepetini tamamen temizledi. User: {UserId}", userId);
            }

            return RedirectToAction("MyCart");
        }

        [HttpGet]
        public IActionResult OrderSuccess()
        {
            if (IsAdmin())
            {
                return Forbid();
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (IsAdmin())
            {
                return Forbid();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            _logger.LogInformation("Kullanıcı siparişlerini görüntüledi. User: {UserId}, Sipariş Sayısı: {OrderCount}", userId, orders.Count);

            return View(orders);
        }


        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                _logger.LogWarning("Kullanıcı kendi olmayan ya da olmayan bir sipariş detayını görüntülemeye çalıştı. User: {UserId}, OrderId: {OrderId}", userId, id);
                return NotFound();
            }

            _logger.LogInformation("Kullanıcı bir sipariş detayını görüntüledi. User: {UserId}, OrderId: {OrderId}", userId, id);

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> IncreaseQuantityAjax(int id)
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin işlem yapamaz." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new { success = false, error = "Yetkisiz" });

            var cartItem = await _context.CartItems.Include(ci => ci.Cart).Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);
            if (cartItem == null)
                return Json(new { success = false, error = "Ürün bulunamadı" });

            if (cartItem.Product != null && cartItem.Quantity < cartItem.Product.Stock)
            {
                cartItem.Quantity++;
                await _context.SaveChangesAsync();

                var lineTotal = cartItem.Quantity * cartItem.Product.Price;
                var cartTotal = await _context.CartItems.Where(x => x.CartId == cartItem.CartId).SumAsync(x => x.Product.Price * x.Quantity);

                _logger.LogInformation("AJAX - Ürün adedi artırıldı. User: {UserId}, ProductId: {ProductId}, Yeni Adet: {Quantity}", userId, cartItem.ProductId, cartItem.Quantity);

                return Json(new { success = true, quantity = cartItem.Quantity, lineTotal, cartTotal });
            }

            _logger.LogWarning("AJAX - Stok yetersiz, ürün adedi artırılamadı. User: {UserId}, ProductId: {ProductId}", userId, cartItem.ProductId);

            return Json(new { success = false, error = "Stok yetersiz" });
        }

        [HttpPost]
        public async Task<IActionResult> DecreaseQuantityAjax(int id)
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin işlem yapamaz." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new { success = false, error = "Yetkisiz" });

            var cartItem = await _context.CartItems.Include(ci => ci.Cart).Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);

            if (cartItem == null)
                return Json(new { success = false, error = "Ürün bulunamadı" });

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                await _context.SaveChangesAsync();

                var lineTotal = cartItem.Quantity * cartItem.Product.Price;
                var cartTotal = await _context.CartItems.Where(x => x.CartId == cartItem.CartId)
                                        .SumAsync(x => x.Product.Price * x.Quantity);

                _logger.LogInformation("AJAX - Ürün adedi azaltıldı. User: {UserId}, ProductId: {ProductId}, Yeni Adet: {Quantity}", userId, cartItem.ProductId, cartItem.Quantity);

                return Json(new { success = true, quantity = cartItem.Quantity, lineTotal, cartTotal });
            }
            else
            {
                var cartId = cartItem.CartId;
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                var cartTotal = await _context.CartItems.Where(x => x.CartId == cartId).SumAsync(x => x.Product.Price * x.Quantity);

                _logger.LogInformation("AJAX - Sepetten ürün tamamen silindi (adet 1 idi). User: {UserId}, ProductId: {ProductId}", userId, cartItem.ProductId);

                return Json(new { success = true, quantity = 0, lineTotal = 0, cartTotal });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            int? defaultAddressId = addresses.FirstOrDefault(a => a.IsDefault)?.Id;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var viewModel = new CheckoutViewModel
            {
                Addresses = addresses,
                SelectedAddressId = defaultAddressId,
                CartItems = cart?.CartItems.ToList() ?? new List<CartItem>()
            };

            _logger.LogInformation("Kullanıcı checkout ekranını açtı. User: {UserId}", userId);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (IsAdmin()) return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (model.SelectedAddressId == null)
            {
                model.Addresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                model.CartItems = cart?.CartItems.ToList() ?? new List<CartItem>();
                ModelState.AddModelError("SelectedAddressId", "Lütfen adres seçiniz.");
                _logger.LogWarning("Checkout: Adres seçilmedi. User: {UserId}", userId);
                return View(model);
            }

            var cartDb = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cartDb == null)
            {
                _logger.LogWarning("Checkout: Cart bulunamadı. User: {UserId}", userId);
                return RedirectToAction("MyCart", "Cart");
            }

            var cartItems = await _context.CartItems.Where(x => x.CartId == cartDb.Id).Include(x => x.Product).ToListAsync();
            if (cartItems == null || !cartItems.Any())
            {
                _logger.LogWarning("Checkout: Sepet boş. User: {UserId}", userId);
                return RedirectToAction("MyCart", "Cart");
            }

            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == model.SelectedAddressId && a.UserId == userId);
            if (address == null)
            {
                TempData["Error"] = "Adres bulunamadı";
                _logger.LogWarning("Checkout: Adres bulunamadı. User: {UserId}, AddressId: {AddressId}", userId, model.SelectedAddressId);
                return RedirectToAction("Checkout");
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Beklemede,
                TotalAmount = cartItems.Sum(i => i.Product.Price * i.Quantity),
                AddressId = address.Id,
                AddressSnapshot = $"{address.Title} - {address.FullAddress}, {address.District}, {address.City} {address.ZipCode}"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                _context.OrderItems.Add(orderItem);

                if (item.Product.Stock >= item.Quantity)
                {
                    item.Product.Stock -= item.Quantity;
                }
                else
                {
                    TempData["Error"] = $"Ürün stok yetersiz: {item.Product.Name}";
                    _logger.LogError("Checkout: Stok yetersiz! User: {UserId}, ProductId: {ProductId}, İstenen: {Quantity}, Stok: {Stock}",
                        userId, item.ProductId, item.Quantity, item.Product.Stock);
                    return RedirectToAction("MyCart", "Cart");
                }
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["OrderSuccess"] = "Siparişiniz başarıyla oluşturuldu!";
            _logger.LogInformation("Checkout: Sipariş başarıyla oluşturuldu. User: {UserId}, OrderId: {OrderId}, Toplam: {Total}",
                userId, order.Id, order.TotalAmount);

            await _hubContext.Clients.All.SendAsync("ReceiveOrderNotification", "Yeni sipariş alındı!");

            return RedirectToAction("OrderSuccess", "Cart");
        }


        [HttpPost]
        public async Task<IActionResult> RemoveFromCartAjax(int id)
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin işlem yapamaz." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new { success = false, error = "Yetkisiz" });

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);

            if (cartItem == null)
                return Json(new { success = false, error = "Ürün bulunamadı" });

            var cartId = cartItem.CartId;
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            var cartTotal = await _context.CartItems
                .Where(x => x.CartId == cartId)
                .SumAsync(x => x.Product.Price * x.Quantity);

            _logger.LogInformation("AJAX - Ürün sepetten çıkarıldı. User: {UserId}, ProductId: {ProductId}", userId, cartItem.ProductId);

            return Json(new { success = true, cartTotal });
        }

        [HttpPost]
        public async Task<IActionResult> ClearCartAjax()
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin işlem yapamaz." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new { success = false, error = "Yetkisiz" });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart?.CartItems.Any() == true)
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
                _logger.LogInformation("AJAX - Kullanıcı sepetini tamamen temizledi. User: {UserId}", userId);
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> SideCartPartial()
        {
            if (IsAdmin()) return PartialView("SideCartPartial", new List<CartItem>());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return PartialView("SideCartPartial", new List<CartItem>());

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var cartItems = cart?.CartItems.ToList() ?? new List<CartItem>();

            _logger.LogInformation("SideCartPartial çağrıldı. User: {UserId}, Ürün Sayısı: {Count}", userId, cartItems.Count);

            return PartialView("SideCartPartial", cartItems);
        }
    }
}
