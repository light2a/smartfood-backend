using SmartFoodAPI.ValidationAttributes;

namespace SmartFoodAPI.DTOs.Auth
{
    public class RegisterRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        [CustomPhoneValidation]
        public string PhoneNumber { get; set; }
        [CustomPasswordValidation]
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
