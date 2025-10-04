using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

public interface IImageService
{
    Task<string> UploadAsync(IFormFile file);
}

public class AnhMoeImageService : IImageService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AnhMoeImageService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AnhMoe:ApiKey"]!; // đọc từ appsettings.json
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        var requestUrl = $"https://anh.moe/api/1/upload/?key={_apiKey}&format=json";

        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);

        content.Add(fileContent, "source", file.FileName);

        var response = await _httpClient.PostAsync(requestUrl, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Upload failed: {body}");

        var result = JsonSerializer.Deserialize<AnhMoeResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result?.Image?.Url == null)
            throw new Exception($"Upload failed, response: {body}");

        return result.Image.Url;
    }


    private class AnhMoeResponse
    {
        public int Status_Code { get; set; }
        public string Status_Txt { get; set; } = "";
        public ImageInfo? Image { get; set; }
    }

    private class ImageInfo
    {
        public string Name { get; set; } = "";
        public string Extension { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
