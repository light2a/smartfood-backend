using SmartFoodAPI.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace SmartFoodAPI.DTOs.Auth
{
    public class UpdateAccountRequest
    {
        public string FullName { get; set; }

        [DataType(DataType.PhoneNumber)]
        [CustomPhoneValidation]
        public string PhoneNumber { get; set; }
    }
}
