using DAL.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Seller
{
    public class UpdateSellerBankInfoRequestDto
    {
        [Required]
        [StringLength(20, MinimumLength = 5)]
        public string BankAccountNumber { get; set; }

        [Required]
        public VietnameseBankCode BankCode { get; set; }
    }
}
