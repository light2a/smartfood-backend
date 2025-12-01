namespace BLL.DTOs.Order
{
    public class CalculateShippingFeeRequest
    {
        public int RestaurantId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
