using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifico.Services;
using Notifico.Models;
using Notifico.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Notifico.Hubs;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Notifico.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            IHubContext<NotificationHub> hubContext,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _hubContext = hubContext;
            _logger = logger;
        }

        private bool IsAdmin() => User.IsInRole("Admin");
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId)
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var result = await _cartService.AddToCartAsync(userId, productId);

            if (result.success)
            {
                TempData["Success"] = "Ürün sepete eklendi!";
            }
            else
            {
                TempData["Error"] = result.error ?? "Stok yetersiz veya ürün bulunamadı!";
            }
            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        public async Task<IActionResult> AddToCartAjax(int productId)
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin kullanıcılar sepete ürün ekleyemez!" });
            var userId = GetUserId();
            if (userId == null) return Json(new { success = false, error = "Giriş yapmalısınız" });

            var result = await _cartService.AddToCartAsync(userId, productId);
            return Json(new { success = result.success, error = result.error });
        }

        [HttpGet]
        public async Task<IActionResult> MyCart()
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartItems = await _cartService.GetCartItemsAsync(userId);
            _logger.LogInformation("Kullanıcı sepetini görüntüledi. User: {UserId}, Ürün Sayısı: {Count}", userId, cartItems.Count);
            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            await _cartService.RemoveFromCartAsync(userId, id);
            return RedirectToAction("MyCart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecreaseQuantity(int id)
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            await _cartService.DecreaseQuantityAsync(userId, id);
            return RedirectToAction("MyCart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncreaseQuantity(int id)
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            await _cartService.IncreaseQuantityAsync(userId, id);
            return RedirectToAction("MyCart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            await _cartService.ClearCartAsync(userId);
            return RedirectToAction("MyCart");
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var viewModel = await _cartService.GetCheckoutViewModelAsync(userId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            if (model.SelectedAddressId == null)
            {
                model = await _cartService.GetCheckoutViewModelAsync(userId);
                ModelState.AddModelError("SelectedAddressId", "Lütfen adres seçiniz.");
                return View(model);
            }

            var (success, error, orderId) = await _cartService.CheckoutAsync(userId, model.SelectedAddressId.Value);

            if (!success)
            {
                model = await _cartService.GetCheckoutViewModelAsync(userId);
                TempData["Error"] = error;
                return View(model);
            }

            await _hubContext.Clients.All.SendAsync("ReceiveOrderNotification", "Yeni sipariş alındı!");
            TempData["OrderSuccess"] = "Siparişiniz başarıyla oluşturuldu!";
            return RedirectToAction("OrderSuccess");
        }

        [HttpGet]
        public IActionResult OrderSuccess()
        {
            if (IsAdmin()) return Forbid();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = await _cartService.GetOrdersAsync(userId);
            _logger.LogInformation("Kullanıcı siparişlerini görüntüledi. User: {UserId}, Sipariş Sayısı: {OrderCount}", userId, orders.Count);
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            if (IsAdmin()) return Forbid();
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _cartService.GetOrderDetailAsync(userId, id);
            if (order == null) return NotFound();

            _logger.LogInformation("Kullanıcı bir sipariş detayını görüntüledi. User: {UserId}, OrderId: {OrderId}", userId, id);
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadOrderPdf(int id)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _cartService.GetOrderDetailAsync(userId, id);
            if (order == null) return NotFound();

            string address = order.AddressSnapshot ?? order.Address?.FullAddress ?? "Adres bulunamadı";
            var fileName = $"Siparis_{order.Id}_{order.OrderDate:yyyyMMddHHmm}.pdf";
            var pdfBytes = await _cartService.GenerateOrderPdfAsync(order, address, User.Identity.Name);

            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> IncreaseQuantityAjax(int id)
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin işlem yapamaz." });
            var userId = GetUserId();
            if (userId == null) return Json(new { success = false, error = "Yetkisiz" });

            var result = await _cartService.IncreaseQuantityAjaxAsync(userId, id);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> DecreaseQuantityAjax(int id)
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin işlem yapamaz." });
            var userId = GetUserId();
            if (userId == null) return Json(new { success = false, error = "Yetkisiz" });

            var result = await _cartService.DecreaseQuantityAjaxAsync(userId, id);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCartAjax(int id)
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin işlem yapamaz." });
            var userId = GetUserId();
            if (userId == null) return Json(new { success = false, error = "Yetkisiz" });

            var result = await _cartService.RemoveFromCartAsync(userId, id);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> ClearCartAjax()
        {
            if (IsAdmin()) return Json(new { success = false, error = "Admin işlem yapamaz." });
            var userId = GetUserId();
            if (userId == null) return Json(new { success = false, error = "Yetkisiz" });

            await _cartService.ClearCartAsync(userId);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> SideCartPartial()
        {
            if (IsAdmin()) return PartialView("SideCartPartial", new List<CartItem>());
            var userId = GetUserId();
            if (userId == null) return PartialView("SideCartPartial", new List<CartItem>());

            var cartItems = await _cartService.GetCartItemsForPartialAsync(userId);
            return PartialView("SideCartPartial", cartItems);
        }
    }
}
