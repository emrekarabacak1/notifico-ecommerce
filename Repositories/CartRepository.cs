using Notifico.Data;
using Notifico.Models;
using Microsoft.EntityFrameworkCore;

namespace Notifico.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;
        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> GetCartByUserIdAsync(string userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task AddCartAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
        }

        public async Task AddCartItemAsync(CartItem item)
        {
            await _context.CartItems.AddAsync(item);
        }

        public async Task<CartItem> GetCartItemByIdAsync(int id, string userId)
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);
        }

        public async Task<CartItem> GetCartItemByProductAsync(int productId, string userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            return cart?.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        }

        public async Task<List<CartItem>> GetCartItemsByCartIdAsync(int cartId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();
        }

        public async Task RemoveCartItemAsync(CartItem item)
        {
            _context.CartItems.Remove(item);
        }

        public async Task RemoveCartItemsAsync(IEnumerable<CartItem> items)
        {
            _context.CartItems.RemoveRange(items);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Product> GetProductByIdAsync(int productId)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<List<CartItem>> GetCartItemsWithProductsAsync(string userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            return cart?.CartItems.ToList() ?? new List<CartItem>();
        }
        public async Task<decimal> GetCartTotalAsync(int cartId)
        {
            return await _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .SumAsync(ci => ci.Product.Price * ci.Quantity);
        }

        public async Task<Order> GetOrderByIdAsync(int id, string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(string userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Address>> GetAddressesByUserIdAsync(string userId)
        {
            return await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task<Address> GetAddressByIdAsync(int addressId, string userId)
        {
            return await _context.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
        }

        public async Task AddOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task AddOrderItemAsync(OrderItem item)
        {
            await _context.OrderItems.AddAsync(item);
        }
    }
}
