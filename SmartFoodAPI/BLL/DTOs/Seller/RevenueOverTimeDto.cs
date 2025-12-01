namespace BLL.DTOs.Seller
{
    public class RevenueOverTimeDto
    {
        public string Period { get; set; } // e.g., "2023-10-26" or "2023-10"
        public decimal Revenue { get; set; }
    }
}
