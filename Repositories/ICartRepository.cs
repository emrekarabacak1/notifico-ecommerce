using Notifico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifico.Repositories
{
    public interface ICartRepository
    {
        Task<Cart> GetCartByUserIdAsync(string userId);
        Task AddCartAsync(Cart cart);
        Task AddCartItemAsync(CartItem item);
        Task<CartItem> GetCartItemByIdAsync(int id, string userId);
        Task<CartItem> GetCartItemByProductAsync(int productId, string userId);
        Task<List<CartItem>> GetCartItemsByCartIdAsync(int cartId);
        Task RemoveCartItemAsync(CartItem item);
        Task RemoveCartItemsAsync(IEnumerable<CartItem> items);
        Task SaveChangesAsync();
        Task<Product> GetProductByIdAsync(int productId);
        Task<List<CartItem>> GetCartItemsWithProductsAsync(string userId);
        Task<decimal> GetCartTotalAsync(int cartId);
        Task<Order> GetOrderByIdAsync(int id, string userId);
        Task<List<Order>> GetOrdersByUserIdAsync(string userId);
        Task<List<Address>> GetAddressesByUserIdAsync(string userId);
        Task<Address> GetAddressByIdAsync(int addressId, string userId);
        Task AddOrderAsync(Order order);
        Task AddOrderItemAsync(OrderItem item);
    }
}
