using Notifico.Models;

namespace Notifico.Services
{
    public interface IAdminService
    {
        Task<ViewModel> GetDashboardViewModelAsync();
        Task<List<Product>> GetLowStockProductsAsync(int threshold = 10);
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<bool> AddProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<List<Order>> GetAllOrdersAsync(string search, OrderStatus? status);
        Task<Order> GetOrderByIdAsync(int id);
        Task<bool> UpdateOrderStatusAsync(int id, OrderStatus status);
        Task<bool> DeleteOrderAsync(int id);
        Task<bool> CancelOrderAsync(int id);
    }
}
