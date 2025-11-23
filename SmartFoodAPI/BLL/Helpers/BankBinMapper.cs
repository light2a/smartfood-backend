using DAL.Models.Enums;

namespace BLL.Helpers
{
    /// <summary>
    /// Maps Vietnamese bank codes to their BIN (Bank Identification Number) codes for PayOS
    /// Since your enum already contains the bank codes, we just need to convert them to strings
    /// </summary>
    public static class BankBinMapper
    {
        /// <summary>
        /// Gets the BIN code for a given Vietnamese bank code
        /// The enum values are already the bank BIN codes, so we just convert to string
        /// </summary>
        /// <param name="bankCode">The bank code enum</param>
        /// <returns>The BIN code as string, or null if not provided</returns>
        public static string? GetBinCode(VietnameseBankCode? bankCode)
        {
            if (!bankCode.HasValue)
            {
                return null;
            }

            // The enum value IS the BIN code, just convert to string
            return ((int)bankCode.Value).ToString();
        }

        /// <summary>
        /// Checks if a bank code has a valid BIN
        /// </summary>
        public static bool HasBinCode(VietnameseBankCode? bankCode)
        {
            return bankCode.HasValue;
        }
    }
}