using Notifico.Models;
using Notifico.Repositories;

namespace Notifico.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;

        public AdminService(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }
        public async Task<ViewModel> GetDashboardViewModelAsync()
        {
            var totalOrderCount = await _adminRepository.GetOrderCountAsync();
            var totalSales = await _adminRepository.GetTotalSalesAsync();
            var totalCustomerCount = await _adminRepository.GetTotalCustomerCountAsync();
            var recentOrders = await _adminRepository.GetRecentOrdersAsync(5);
            var topProducts = await _adminRepository.GetTopProductsAsync(5);
            var categorySales = await _adminRepository.GetCategorySalesAsync();
            var chartLabels = await _adminRepository.GetChartLabelsAsync(6);
            var chartData = await _adminRepository.GetChartDataAsync(6);

            return new ViewModel
            {
                TotalOrderCount = totalOrderCount,
                TotalSales = totalSales,
                TotalCustomerCount = totalCustomerCount,
                RecentOrders = recentOrders,
                ChartLabels = chartLabels,
                ChartData = chartData,
                TopProducts = topProducts,
                CategorySales = categorySales
            };
        }

        public async Task<List<Product>> GetLowStockProductsAsync(int threshold = 10)
            => await _adminRepository.GetLowStockProductsAsync(threshold);

        public async Task<List<Product>> GetAllProductsAsync()
            => await _adminRepository.GetAllProductsAsync();

        public async Task<Product> GetProductByIdAsync(int id)
            => await _adminRepository.GetProductByIdAsync(id);

        public async Task<bool> AddProductAsync(Product product)
        {
            await _adminRepository.AddProductAsync(product);
            await _adminRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            await _adminRepository.UpdateProductAsync(product);
            await _adminRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            await _adminRepository.DeleteProductAsync(id);
            await _adminRepository.SaveChangesAsync();
            return true;
        }
        public async Task<List<Order>> GetAllOrdersAsync(string search, OrderStatus? status)
            => await _adminRepository.GetAllOrdersAsync(search, status);

        public async Task<Order> GetOrderByIdAsync(int id)
            => await _adminRepository.GetOrderByIdAsync(id);

        public async Task<bool> UpdateOrderStatusAsync(int id, OrderStatus status)
        {
            await _adminRepository.UpdateOrderStatusAsync(id, status);
            await _adminRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            await _adminRepository.DeleteOrderAsync(id);
            await _adminRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(int id)
        {
            await _adminRepository.CancelOrderAsync(id);
            await _adminRepository.SaveChangesAsync();
            return true;
        }
    }
}
