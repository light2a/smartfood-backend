using DAL.Models.Enums;

namespace BLL.DTOs.Seller
{
    public class SellerBankInfoResponse
    {
        public string BankAccountNumber { get; set; }
        public VietnameseBankCode? BankCode { get; set; }
        public string BankName { get; set; }
    }
}
