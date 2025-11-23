using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Order
{
    public class CreateOrderResponse
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal FinalAmount { get; set; }

        /// <summary>
        /// PayOS payment URL for the customer to complete payment
        /// </summary>
        public string PaymentUrl { get; set; }

        public string Message { get; set; }
    }
}

