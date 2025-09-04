using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Notifico.Data;
using Notifico.Hubs;
using Notifico.Models;
using Notifico.ViewModels;
using System.Security.Claims;

namespace Notifico.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CartController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Helper: ADMIN ise erişimi engelle
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
                    return RedirectToAction("Index", "Product");
                }
                cartItem.Quantity++;
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
                return Json(new { success = false, error = "Ürün bulunamadı" });

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
                    return Json(new { success = false, error = "Stok yetersiz" });

                cartItem.Quantity++;
            }
            else
            {
                cartItem = new CartItem { CartId = cart.Id, ProductId = productId, Quantity = 1 };
                await _context.CartItems.AddAsync(cartItem);
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true });
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
            }
            else
            {
                _context.CartItems.Remove(cartItem);
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
                return NotFound();

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

                return Json(new { success = true, quantity = cartItem.Quantity, lineTotal, cartTotal });
            }

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

                return Json(new { success = true, quantity = cartItem.Quantity, lineTotal, cartTotal });
            }
            else
            {
                var cartId = cartItem.CartId;
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                var cartTotal = await _context.CartItems.Where(x => x.CartId == cartId).SumAsync(x => x.Product.Price * x.Quantity);

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
                return View(model);
            }

            var cartDb = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cartDb == null)
                return RedirectToAction("MyCart", "Cart");

            var cartItems = await _context.CartItems.Where(x => x.CartId == cartDb.Id).Include(x => x.Product).ToListAsync();
            if (cartItems == null || !cartItems.Any())
                return RedirectToAction("MyCart", "Cart");

            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == model.SelectedAddressId && a.UserId == userId);
            if (address == null)
            {
                TempData["Error"] = "Adres bulunamadı";
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
                    return RedirectToAction("MyCart", "Cart");
                }
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["OrderSuccess"] = "Siparişiniz başarıyla oluşturuldu!";
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
            return PartialView("SideCartPartial", cartItems);
        }
    }
}
