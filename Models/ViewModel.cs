namespace Notifico.Models
{
    public class ProductSalesInfo
    {
        public string ProductName { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class CategorySalesInfo
    {
        public string CategoryName { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ViewModel
    {
        public int TotalOrderCount { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalCustomerCount { get; set; }
        public List<Order> RecentOrders { get; set; }
        public List<string> ChartLabels { get; set; }
        public List<decimal> ChartData { get; set; }
        public List<ProductSalesInfo> TopProducts { get; set; }
        public List<CategorySalesInfo> CategorySales { get; set; }
    }
}
