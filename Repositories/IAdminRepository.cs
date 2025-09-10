using Notifico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifico.Repositories
{
    public interface IAdminRepository
    {
        Task<int> GetProductCountAsync();
        Task<int> GetOrderCountAsync();
        Task<decimal> GetTotalSalesAsync();
        Task<int> GetTotalCustomerCountAsync();
        Task<List<Order>> GetRecentOrdersAsync(int count);
        Task<List<ProductSalesInfo>> GetTopProductsAsync(int count);
        Task<List<CategorySalesInfo>> GetCategorySalesAsync();
        Task<List<string>> GetChartLabelsAsync(int months);
        Task<List<decimal>> GetChartDataAsync(int months);
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        Task<List<Product>> GetLowStockProductsAsync(int threshold);
        Task<List<Order>> GetAllOrdersAsync(string search, OrderStatus? status);
        Task<Order> GetOrderByIdAsync(int id);
        Task UpdateOrderStatusAsync(int id, OrderStatus status);
        Task DeleteOrderAsync(int id);
        Task CancelOrderAsync(int id);
        Task<List<string>> GetAllCategoriesAsync();
        Task SaveChangesAsync();
    }
}
