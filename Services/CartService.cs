using Notifico.Models;
using Notifico.Repositories;
using Notifico.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace Notifico.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartService(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<Cart> GetOrCreateCartAsync(string userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _cartRepository.AddCartAsync(cart);
                await _cartRepository.SaveChangesAsync();
            }
            return cart;
        }

        public async Task<(bool success, string error)> AddToCartAsync(string userId, int productId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var product = await _cartRepository.GetProductByIdAsync(productId);
            if (product == null)
                return (false, "Ürün bulunamadı.");
            if (product.Stock < 1)
                return (false, "Stok yetersiz.");

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem != null)
            {
                if (product.Stock <= cartItem.Quantity)
                    return (false, "Sepetteki ürün adedi, mevcut stoktan fazla olamaz!");
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
                await _cartRepository.AddCartItemAsync(cartItem);
            }
            await _cartRepository.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> RemoveFromCartAsync(string userId, int cartItemId)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId, userId);
            if (cartItem == null) return false;
            await _cartRepository.RemoveCartItemAsync(cartItem);
            await _cartRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IncreaseQuantityAsync(string userId, int cartItemId)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId, userId);
            if (cartItem == null) return false;

            if (cartItem.Product != null && cartItem.Quantity < cartItem.Product.Stock)
            {
                cartItem.Quantity++;
                await _cartRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> DecreaseQuantityAsync(string userId, int cartItemId)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId, userId);
            if (cartItem == null) return false;

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                await _cartRepository.SaveChangesAsync();
            }
            else
            {
                await _cartRepository.RemoveCartItemAsync(cartItem);
                await _cartRepository.SaveChangesAsync();
            }
            return true;
        }

        public async Task ClearCartAsync(string userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart?.CartItems.Any() == true)
            {
                await _cartRepository.RemoveCartItemsAsync(cart.CartItems);
                await _cartRepository.SaveChangesAsync();
            }
        }

        public async Task<List<CartItem>> GetCartItemsAsync(string userId)
        {
            return await _cartRepository.GetCartItemsWithProductsAsync(userId);
        }

        public async Task<List<CartItem>> GetCartItemsForPartialAsync(string userId)
        {
            return await _cartRepository.GetCartItemsWithProductsAsync(userId);
        }

        public async Task<CheckoutViewModel> GetCheckoutViewModelAsync(string userId)
        {
            var addresses = await _cartRepository.GetAddressesByUserIdAsync(userId);
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            int? defaultAddressId = addresses.FirstOrDefault(a => a.IsDefault)?.Id;

            return new CheckoutViewModel
            {
                Addresses = addresses,
                SelectedAddressId = defaultAddressId,
                CartItems = cart?.CartItems.ToList() ?? new List<CartItem>()
            };
        }

        public async Task<(bool success, string error, int orderId)> CheckoutAsync(string userId, int addressId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null || !cart.CartItems.Any())
                return (false, "Sepetiniz boş.", 0);

            var address = await _cartRepository.GetAddressByIdAsync(addressId, userId);
            if (address == null)
                return (false, "Adres bulunamadı.", 0);

            decimal totalAmount = cart.CartItems.Sum(i => i.Product.Price * i.Quantity);

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Beklemede,
                TotalAmount = totalAmount,
                AddressId = address.Id,
                AddressSnapshot = $"{address.Title} - {address.FullAddress}, {address.District}, {address.City} {address.ZipCode}"
            };

            await _cartRepository.AddOrderAsync(order);
            await _cartRepository.SaveChangesAsync();

            foreach (var item in cart.CartItems)
            {
                if (item.Product.Stock < item.Quantity)
                    return (false, $"Ürün stok yetersiz: {item.Product.Name}", 0);

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                await _cartRepository.AddOrderItemAsync(orderItem);

                item.Product.Stock -= item.Quantity;
            }
            await _cartRepository.RemoveCartItemsAsync(cart.CartItems);
            await _cartRepository.SaveChangesAsync();
            return (true, null, order.Id);
        }

        public async Task<List<Order>> GetOrdersAsync(string userId)
        {
            return await _cartRepository.GetOrdersByUserIdAsync(userId);
        }

        public async Task<Order> GetOrderDetailAsync(string userId, int orderId)
        {
            return await _cartRepository.GetOrderByIdAsync(orderId, userId);
        }

        public async Task<byte[]> GenerateOrderPdfAsync(Order order, string address, string userName)
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

        public async Task<object> IncreaseQuantityAjaxAsync(string userId, int cartItemId)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId, userId);
            if (cartItem == null)
                return new { success = false, error = "Ürün bulunamadı" };

            if (cartItem.Product != null && cartItem.Quantity < cartItem.Product.Stock)
            {
                cartItem.Quantity++;
                await _cartRepository.SaveChangesAsync();

                var lineTotal = cartItem.Quantity * cartItem.Product.Price;
                var cartTotal = await _cartRepository.GetCartTotalAsync(cartItem.CartId);

                return new { success = true, quantity = cartItem.Quantity, lineTotal, cartTotal };
            }
            return new { success = false, error = "Stok yetersiz" };
        }

        public async Task<object> DecreaseQuantityAjaxAsync(string userId, int cartItemId)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId, userId);
            if (cartItem == null)
                return new { success = false, error = "Ürün bulunamadı" };

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                await _cartRepository.SaveChangesAsync();

                var lineTotal = cartItem.Quantity * cartItem.Product.Price;
                var cartTotal = await _cartRepository.GetCartTotalAsync(cartItem.CartId);

                return new { success = true, quantity = cartItem.Quantity, lineTotal, cartTotal };
            }
            else
            {
                var cartId = cartItem.CartId;
                await _cartRepository.RemoveCartItemAsync(cartItem);
                await _cartRepository.SaveChangesAsync();

                var cartTotal = await _cartRepository.GetCartTotalAsync(cartId);

                return new { success = true, quantity = 0, lineTotal = 0, cartTotal };
            }
        }
    }
}
