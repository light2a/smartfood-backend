namespace SmartFoodAPI.Common
{
    public class ErrorResponse
    {
        public string ErrorCode { get; set; } = "";
        public string Message { get; set; } = "";

        public static ErrorResponse FromStatus(int statusCode, string message) =>
            new ErrorResponse
            {
                ErrorCode = statusCode switch
                {
                    400 => "PR40001",
                    401 => "PR40101",
                    403 => "PR40301",
                    404 => "PR40401",
                    _ => "PR50001"
                },
                Message = message
            };
    }
}
