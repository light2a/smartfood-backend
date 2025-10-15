using SmartFoodAPI.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace SmartFoodAPI.DTOs.Auth
{
    public class RegisterSellerRequest
    {
        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;

        [Required]
        [CustomPasswordValidation] // ✅ use your custom password rule
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("Password", ErrorMessage = "Password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = null!;

        [CustomPhoneValidation] // ✅ use your custom phone rule
        public string? PhoneNumber { get; set; }
    }
}
