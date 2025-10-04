﻿namespace SmartFoodAPI.DTOs.Auth
{
    public class RegisterSellerRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string DisplayName { get; set; } = null!; // For Seller entity
    }
}
