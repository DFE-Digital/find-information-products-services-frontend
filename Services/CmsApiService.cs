using System.Text.Json;
using System.Text;

namespace FipsFrontend.Services;

public class CmsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CmsApiService> _logger;
    private readonly string _baseUrl;

    public CmsApiService(HttpClient httpClient, IConfiguration configuration, ILogger<CmsApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            
            // Use read API key for GET requests
            var readApiKey = _configuration["CmsApi:ReadApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", readApiKey);
            
            var response = await _httpClient.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Use write API key for POST requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            
            var response = await _httpClient.PostAsync(fullUrl, content);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Use write API key for PUT requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            
            var response = await _httpClient.PutAsync(fullUrl, content);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            
            // Use write API key for DELETE requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            
            var response = await _httpClient.DeleteAsync(fullUrl);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return false;
        }
    }
}
