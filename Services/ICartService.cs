using Notifico.Models;
using Notifico.ViewModels;

public interface ICartService
{
    Task<(bool success, string error)> AddToCartAsync(string userId, int productId);
    Task<bool> RemoveFromCartAsync(string userId, int cartItemId);
    Task<bool> IncreaseQuantityAsync(string userId, int cartItemId);
    Task<bool> DecreaseQuantityAsync(string userId, int cartItemId);
    Task ClearCartAsync(string userId);
    Task<List<CartItem>> GetCartItemsAsync(string userId);
    Task<List<CartItem>> GetCartItemsForPartialAsync(string userId);
    Task<CheckoutViewModel> GetCheckoutViewModelAsync(string userId);
    Task<(bool success, string error, int orderId)> CheckoutAsync(string userId, int addressId);
    Task<List<Order>> GetOrdersAsync(string userId);
    Task<Order> GetOrderDetailAsync(string userId, int orderId);
    Task<byte[]> GenerateOrderPdfAsync(Order order, string address, string userName);
    Task<object> IncreaseQuantityAjaxAsync(string userId, int cartItemId);
    Task<object> DecreaseQuantityAjaxAsync(string userId, int cartItemId);
}
