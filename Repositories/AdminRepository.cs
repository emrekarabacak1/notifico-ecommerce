using Notifico.Data;
using Notifico.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Notifico.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AdminRepository(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<int> GetProductCountAsync()
            => await _context.Products.CountAsync();

        public async Task<int> GetOrderCountAsync()
            => await _context.Orders.CountAsync();

        public async Task<decimal> GetTotalSalesAsync()
            => await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        public async Task<int> GetTotalCustomerCountAsync()
            => (await _userManager.GetUsersInRoleAsync("User")).Count;

        public async Task<List<Order>> GetRecentOrdersAsync(int count)
            => await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToListAsync();

        public async Task<List<ProductSalesInfo>> GetTopProductsAsync(int count)
            => await _context.OrderItems
                .Include(oi => oi.Product)
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new ProductSalesInfo
                {
                    ProductName = g.Key.Name,
                    TotalSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(count)
                .ToListAsync();

        public async Task<List<CategorySalesInfo>> GetCategorySalesAsync()
            => await _context.OrderItems
                .Include(oi => oi.Product)
                .GroupBy(oi => oi.Product.Category)
                .Select(g => new CategorySalesInfo
                {
                    CategoryName = g.Key,
                    TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

        public async Task<List<string>> GetChartLabelsAsync(int months)
        {
            var chartLabels = new List<string>();
            for (int i = months - 1; i >= 0; i--)
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
                chartLabels.Add(monthStart.ToString("MMMM yyyy", new CultureInfo("tr-TR")));
            }
            return chartLabels;
        }

        public async Task<List<decimal>> GetChartDataAsync(int months)
        {
            var chartData = new List<decimal>();
            for (int i = months - 1; i >= 0; i--)
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
                monthStart = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);  
                var monthEnd = monthStart.AddMonths(1);
                monthEnd = DateTime.SpecifyKind(monthEnd, DateTimeKind.Utc);      

                decimal total = await _context.Orders
                    .Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                chartData.Add(total);
            }

            return chartData;
        }
        public async Task<List<Product>> GetAllProductsAsync()
            => await _context.Products.ToListAsync();

        public async Task<Product> GetProductByIdAsync(int id)
            => await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        public async Task AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }
        }

        public async Task<List<Product>> GetLowStockProductsAsync(int threshold)
            => await _context.Products
                .Where(p => p.Stock <= threshold)
                .OrderBy(p => p.Stock)
                .ToListAsync();
        public async Task<List<Order>> GetAllOrdersAsync(string search, OrderStatus? status)
        {
            var orders = _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                orders = orders.Where(o => o.User.UserName.ToLower().Contains(search.ToLower()));

            if (status.HasValue)
                orders = orders.Where(o => o.Status == status.Value);

            return await orders.ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
            => await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task UpdateOrderStatusAsync(int id, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
            }
        }

        public async Task DeleteOrderAsync(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
            if (order != null)
            {
                if (order.OrderItems != null && order.OrderItems.Any())
                    _context.OrderItems.RemoveRange(order.OrderItems);

                _context.Orders.Remove(order);
            }
        }

        public async Task CancelOrderAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = OrderStatus.IptalEdildi;
            }
        }
        public async Task<List<string>> GetAllCategoriesAsync()
            => await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
