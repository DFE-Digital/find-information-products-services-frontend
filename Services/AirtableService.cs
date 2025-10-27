using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface IAirtableService
{
    Task<bool> SubmitFeedbackAsync(string feedback, string pageUrl, string service = "FIPS", string? userEmail = null);
}

public class AirtableService : IAirtableService
{
    private readonly HttpClient _httpClient;
    private readonly AirtableConfiguration _config;
    private readonly ILogger<AirtableService> _logger;

    public AirtableService(HttpClient httpClient, IOptions<AirtableConfiguration> config, ILogger<AirtableService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        // Configure HttpClient for Airtable API
        _httpClient.BaseAddress = new Uri($"https://api.airtable.com/v0/{_config.BaseId}/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
    }

    public async Task<bool> SubmitFeedbackAsync(string feedback, string pageUrl, string service = "FIPS", string? userEmail = null)
    {
        try
        {
            _logger.LogInformation("AirtableService.SubmitFeedbackAsync called with feedback length: {FeedbackLength}", feedback?.Length ?? 0);
            _logger.LogInformation("Airtable configuration - BaseId: {BaseId}, ApiKey: {ApiKey}, TableName: {TableName}", 
                _config.BaseId, 
                _config.ApiKey?.Substring(0, Math.Min(10, _config.ApiKey.Length)) + "...", 
                _config.FeedbackTableName);

            var payload = new
            {
                fields = new
                {
                    Feedback = feedback,
                    Service = service,
                    URL = pageUrl,
                    UserID = userEmail
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogInformation("JSON payload being sent to Airtable: {JsonPayload}", json);

            _logger.LogInformation("Submitting feedback to Airtable: {Feedback}, Page: {PageUrl}, Service: {Service}, UserEmail: {UserEmail}", 
                feedback, pageUrl, service, userEmail);
            
            _logger.LogInformation("UserID field value being sent to Airtable: {UserID}", userEmail ?? "null");

            var requestUrl = $"{_config.FeedbackTableName}";
            _logger.LogInformation("Making request to Airtable URL: {RequestUrl}", requestUrl);

            var response = await _httpClient.PostAsync(requestUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Feedback submitted to Airtable successfully");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to submit feedback to Airtable. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback to Airtable");
            return false;
        }
    }
}
