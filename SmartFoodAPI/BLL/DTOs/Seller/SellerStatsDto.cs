namespace BLL.DTOs.Seller
{
    public class SellerStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int UniqueCustomers { get; set; }
    }
}
