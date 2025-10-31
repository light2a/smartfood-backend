//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text.Json;
//using Microsoft.Extensions.Configuration;
//using Microsoft.AspNetCore.Http;
//using System.Threading.Tasks;

//public interface IImageService
//{
//    Task<string> UploadAsync(IFormFile file);
//}

//public class ImgBBImageService : IImageService
//{
//    private readonly HttpClient _httpClient;
//    private readonly string _apiKey;

//    public ImgBBImageService(HttpClient httpClient, IConfiguration configuration)
//    {
//        _httpClient = httpClient;
//        _apiKey = configuration["ImgBB:ApiKey"]!;
//    }

//    public async Task<string> UploadAsync(IFormFile file)
//    {
//        if (file == null || file.Length == 0)
//            throw new ArgumentException("No file uploaded");

//        var requestUrl = $"https://api.imgbb.com/1/upload?key={_apiKey}";

//        using var content = new MultipartFormDataContent();
//        using var stream = file.OpenReadStream();
//        var fileContent = new StreamContent(stream);
//        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);

//        content.Add(fileContent, "image", file.FileName);

//        var response = await _httpClient.PostAsync(requestUrl, content);
//        var body = await response.Content.ReadAsStringAsync();

//        if (!response.IsSuccessStatusCode)
//            throw new Exception($"Upload failed: {body}");

//        var result = JsonSerializer.Deserialize<ImgBBResponse>(
//            body,
//            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
//        );

//        if (result?.Data?.Display_Url == null)
//            throw new Exception($"Upload failed, response: {body}");

//        return result.Data.Display_Url;
//    }

//    // Inner class mapping JSON response from imgbb
//    private class ImgBBResponse
//    {
//        public ImgBBData? Data { get; set; }
//        public bool Success { get; set; }
//        public int Status { get; set; }
//    }

//    private class ImgBBData
//    {
//        public string Id { get; set; } = "";
//        public string Title { get; set; } = "";
//        public string Url { get; set; } = "";
//        public string Display_Url { get; set; } = "";
//        public ImgBBImage? Image { get; set; }
//    }

//    private class ImgBBImage
//    {
//        public string Filename { get; set; } = "";
//        public string Url { get; set; } = "";
//        public string Extension { get; set; } = "";
//    }
//}
