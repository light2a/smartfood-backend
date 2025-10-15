using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public enum SellerStatus
    {
        Unavailable = 0, // Waiting for admin approval
        Available = 1    // Approved and active
    }
    public enum MenuItemStatus
    {
        DangBan = 1, // Đang bán
        HetHang = 2, // Hết hàng
        An = 3       // Ẩn
    }
    public enum OrderStatus
    {
        Pending ,        
        Preparing ,     
        Delivering ,       
        Completed ,      
        Cancelled ,     
    }
    public enum OrderType
    {
        Pickup = 1,
        Delivery = 2
    }
}

