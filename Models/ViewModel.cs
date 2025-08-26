namespace Notifico.Models
{
    public class ViewModel
    {
        public int TotalOrderCount { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalCustomerCount { get; set; }
        public List<Order> RecentOrders { get; set; }
    }
}
