using System.Text.Json;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface IServiceAssessmentsService
{
    Task<(List<AssessmentSummary> Assessments, int TotalCount)> GetAssessmentsSummaryAsync(int page = 1, int pageSize = 25, string? searchQuery = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null);
    Task<Assessment?> GetAssessmentByIdAsync(int id, TimeSpan? cacheDuration = null);
    Task<List<string>> GetAvailableTypesAsync(TimeSpan? cacheDuration = null);
    Task<List<string>> GetAvailablePhasesAsync(TimeSpan? cacheDuration = null);
    Task<List<string>> GetAvailableStatusesAsync(TimeSpan? cacheDuration = null);
}

public class ServiceAssessmentsService : IServiceAssessmentsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceAssessmentsService> _logger;
    private readonly IEnhancedCacheService _cacheService;
    private readonly string _baseUrl;
    private readonly string _secretId;

    public ServiceAssessmentsService(HttpClient httpClient, IConfiguration configuration, ILogger<ServiceAssessmentsService> logger, IEnhancedCacheService cacheService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cacheService = cacheService;
        _baseUrl = _configuration["SAS:TenantId"] ?? throw new InvalidOperationException("SAS:TenantId not configured");
        _secretId = _configuration["SAS:SecretId"] ?? throw new InvalidOperationException("SAS:SecretId not configured");
        
        // Set up authentication headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_secretId}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<(List<AssessmentSummary> Assessments, int TotalCount)> GetAssessmentsSummaryAsync(int page = 1, int pageSize = 25, string? searchQuery = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null)
    {
        // Create cache key based on parameters
        var cacheKey = $"assessments_summary_{page}_{pageSize}_{searchQuery ?? "null"}_{string.Join("_", filters?.SelectMany(f => f.Value) ?? new string[0])}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<(List<AssessmentSummary> Assessments, int TotalCount)?>(cacheKey);
            if (cachedResult.HasValue)
            {
                _logger.LogDebug("Retrieved assessments summary from cache for key: {CacheKey}", cacheKey);
                return cachedResult.Value;
            }
        }

        try
        {
            // Build query parameters
            var queryParams = new List<string>
            {
                $"pagination[page]={page}",
                $"pagination[pageSize]={pageSize}"
            };

            if (!string.IsNullOrEmpty(searchQuery))
            {
                queryParams.Add($"filters[title][$containsi]={Uri.EscapeDataString(searchQuery)}");
            }

            // Add filters
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    if (filter.Value.Any())
                    {
                        var filterValues = string.Join(",", filter.Value.Select(v => Uri.EscapeDataString(v)));
                        queryParams.Add($"filters[{filter.Key}][$in]={filterValues}");
                    }
                }
            }

            var queryString = string.Join("&", queryParams);
            var url = $"{_baseUrl}api/assessments/published/summary?{queryString}";

            _logger.LogDebug("Fetching assessments summary from: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(jsonContent);

            var assessments = new List<AssessmentSummary>();
            int totalCount = 0;

            if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var assessment = ParseAssessmentSummary(item);
                        if (assessment != null)
                        {
                            assessments.Add(assessment);
                        }
                    }
                }
            }

            if (jsonDocument.RootElement.TryGetProperty("meta", out var metaElement))
            {
                if (metaElement.TryGetProperty("pagination", out var paginationElement))
                {
                    if (paginationElement.TryGetProperty("total", out var totalElement))
                    {
                        totalCount = totalElement.GetInt32();
                    }
                }
            }

            var result = (assessments, totalCount);

            // Cache the result
            if (cacheDuration.HasValue)
            {
                await _cacheService.SetAsync(cacheKey, result, cacheDuration.Value);
                _logger.LogDebug("Cached assessments summary for key: {CacheKey}", cacheKey);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assessments summary from SAS API");
            throw;
        }
    }

    public async Task<Assessment?> GetAssessmentByIdAsync(int id, TimeSpan? cacheDuration = null)
    {
        var cacheKey = $"assessment_detail_{id}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<Assessment?>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Retrieved assessment detail from cache for key: {CacheKey}", cacheKey);
                return cachedResult;
            }
        }

        try
        {
            var url = $"{_baseUrl}api/assessments/{id}";
            _logger.LogDebug("Fetching assessment detail from: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(jsonContent);

            if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
            {
                var assessment = ParseAssessment(dataElement);
                
                // Cache the result
                if (cacheDuration.HasValue && assessment != null)
                {
                    await _cacheService.SetAsync(cacheKey, assessment, cacheDuration.Value);
                    _logger.LogDebug("Cached assessment detail for key: {CacheKey}", cacheKey);
                }
                
                return assessment;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assessment detail from SAS API for ID: {Id}", id);
            throw;
        }
    }

    public async Task<List<string>> GetAvailableTypesAsync(TimeSpan? cacheDuration = null)
    {
        var cacheKey = "assessment_types";
        
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<string>?>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }
        }

        try
        {
            var url = $"{_baseUrl}api/assessments/published/summary?pagination[pageSize]=1000";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(jsonContent);

            var types = new HashSet<string>();

            if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("attributes", out var attributes))
                        {
                            if (attributes.TryGetProperty("type", out var typeElement))
                            {
                                var type = typeElement.GetString();
                                if (!string.IsNullOrEmpty(type))
                                {
                                    types.Add(type);
                                }
                            }
                        }
                    }
                }
            }

            var result = types.OrderBy(t => t).ToList();
            
            if (cacheDuration.HasValue)
            {
                await _cacheService.SetAsync(cacheKey, result, cacheDuration.Value);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available assessment types");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetAvailablePhasesAsync(TimeSpan? cacheDuration = null)
    {
        var cacheKey = "assessment_phases";
        
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<string>?>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }
        }

        try
        {
            var url = $"{_baseUrl}api/assessments/published/summary?pagination[pageSize]=1000";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(jsonContent);

            var phases = new HashSet<string>();

            if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("attributes", out var attributes))
                        {
                            if (attributes.TryGetProperty("phase", out var phaseElement))
                            {
                                var phase = phaseElement.GetString();
                                if (!string.IsNullOrEmpty(phase))
                                {
                                    phases.Add(phase);
                                }
                            }
                        }
                    }
                }
            }

            var result = phases.OrderBy(p => p).ToList();
            
            if (cacheDuration.HasValue)
            {
                await _cacheService.SetAsync(cacheKey, result, cacheDuration.Value);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available assessment phases");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetAvailableStatusesAsync(TimeSpan? cacheDuration = null)
    {
        var cacheKey = "assessment_statuses";
        
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<string>?>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }
        }

        try
        {
            var url = $"{_baseUrl}api/assessments/published/summary?pagination[pageSize]=1000";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(jsonContent);

            var statuses = new HashSet<string>();

            if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("attributes", out var attributes))
                        {
                            if (attributes.TryGetProperty("status", out var statusElement))
                            {
                                var status = statusElement.GetString();
                                if (!string.IsNullOrEmpty(status))
                                {
                                    statuses.Add(status);
                                }
                            }
                        }
                    }
                }
            }

            var result = statuses.OrderBy(s => s).ToList();
            
            if (cacheDuration.HasValue)
            {
                await _cacheService.SetAsync(cacheKey, result, cacheDuration.Value);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available assessment statuses");
            return new List<string>();
        }
    }

    private static AssessmentSummary? ParseAssessmentSummary(JsonElement element)
    {
        try
        {
            if (!element.TryGetProperty("attributes", out var attributes))
                return null;

            var assessment = new AssessmentSummary();

            if (element.TryGetProperty("id", out var idElement))
            {
                assessment.Id = idElement.GetInt32();
            }

            if (attributes.TryGetProperty("documentId", out var documentIdElement))
            {
                assessment.DocumentId = documentIdElement.GetString();
            }

            if (attributes.TryGetProperty("title", out var titleElement))
            {
                assessment.Title = titleElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("type", out var typeElement))
            {
                assessment.Type = typeElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("phase", out var phaseElement))
            {
                assessment.Phase = phaseElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("status", out var statusElement))
            {
                assessment.Status = statusElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("description", out var descriptionElement))
            {
                assessment.Description = descriptionElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("startDate", out var startDateElement) && startDateElement.ValueKind != JsonValueKind.Null)
            {
                if (DateTime.TryParse(startDateElement.GetString(), out var startDate))
                {
                    assessment.StartDate = startDate;
                }
            }

            if (attributes.TryGetProperty("endDate", out var endDateElement) && endDateElement.ValueKind != JsonValueKind.Null)
            {
                if (DateTime.TryParse(endDateElement.GetString(), out var endDate))
                {
                    assessment.EndDate = endDate;
                }
            }

            if (attributes.TryGetProperty("createdAt", out var createdAtElement))
            {
                if (DateTime.TryParse(createdAtElement.GetString(), out var createdAt))
                {
                    assessment.CreatedAt = createdAt;
                }
            }

            if (attributes.TryGetProperty("updatedAt", out var updatedAtElement))
            {
                if (DateTime.TryParse(updatedAtElement.GetString(), out var updatedAt))
                {
                    assessment.UpdatedAt = updatedAt;
                }
            }

            if (attributes.TryGetProperty("publishedAt", out var publishedAtElement) && publishedAtElement.ValueKind != JsonValueKind.Null)
            {
                if (DateTime.TryParse(publishedAtElement.GetString(), out var publishedAt))
                {
                    assessment.PublishedAt = publishedAt;
                }
            }

            if (attributes.TryGetProperty("url", out var urlElement))
            {
                assessment.Url = urlElement.GetString();
            }

            return assessment;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Assessment? ParseAssessment(JsonElement element)
    {
        try
        {
            if (!element.TryGetProperty("attributes", out var attributes))
                return null;

            var assessment = new Assessment();

            if (element.TryGetProperty("id", out var idElement))
            {
                assessment.Id = idElement.GetInt32();
            }

            if (attributes.TryGetProperty("documentId", out var documentIdElement))
            {
                assessment.DocumentId = documentIdElement.GetString();
            }

            if (attributes.TryGetProperty("title", out var titleElement))
            {
                assessment.Title = titleElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("type", out var typeElement))
            {
                assessment.Type = typeElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("phase", out var phaseElement))
            {
                assessment.Phase = phaseElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("status", out var statusElement))
            {
                assessment.Status = statusElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("description", out var descriptionElement))
            {
                assessment.Description = descriptionElement.GetString() ?? string.Empty;
            }

            if (attributes.TryGetProperty("startDate", out var startDateElement) && startDateElement.ValueKind != JsonValueKind.Null)
            {
                if (DateTime.TryParse(startDateElement.GetString(), out var startDate))
                {
                    assessment.StartDate = startDate;
                }
            }

            if (attributes.TryGetProperty("endDate", out var endDateElement) && endDateElement.ValueKind != JsonValueKind.Null)
            {
                if (DateTime.TryParse(endDateElement.GetString(), out var endDate))
                {
                    assessment.EndDate = endDate;
                }
            }

            if (attributes.TryGetProperty("createdAt", out var createdAtElement))
            {
                if (DateTime.TryParse(createdAtElement.GetString(), out var createdAt))
                {
                    assessment.CreatedAt = createdAt;
                }
            }

            if (attributes.TryGetProperty("updatedAt", out var updatedAtElement))
            {
                if (DateTime.TryParse(updatedAtElement.GetString(), out var updatedAt))
                {
                    assessment.UpdatedAt = updatedAt;
                }
            }

            if (attributes.TryGetProperty("publishedAt", out var publishedAtElement) && publishedAtElement.ValueKind != JsonValueKind.Null)
            {
                if (DateTime.TryParse(publishedAtElement.GetString(), out var publishedAt))
                {
                    assessment.PublishedAt = publishedAt;
                }
            }

            if (attributes.TryGetProperty("url", out var urlElement))
            {
                assessment.Url = urlElement.GetString();
            }

            // Parse contacts if available
            if (attributes.TryGetProperty("contacts", out var contactsElement) && contactsElement.ValueKind == JsonValueKind.Array)
            {
                var contacts = new List<AssessmentContact>();
                foreach (var contactElement in contactsElement.EnumerateArray())
                {
                    var contact = new AssessmentContact();
                    if (contactElement.TryGetProperty("name", out var nameElement))
                        contact.Name = nameElement.GetString() ?? string.Empty;
                    if (contactElement.TryGetProperty("email", out var emailElement))
                        contact.Email = emailElement.GetString() ?? string.Empty;
                    if (contactElement.TryGetProperty("role", out var roleElement))
                        contact.Role = roleElement.GetString() ?? string.Empty;
                    if (contactElement.TryGetProperty("department", out var departmentElement))
                        contact.Department = departmentElement.GetString() ?? string.Empty;
                    contacts.Add(contact);
                }
                assessment.Contacts = contacts;
            }

            // Parse attachments if available
            if (attributes.TryGetProperty("attachments", out var attachmentsElement) && attachmentsElement.ValueKind == JsonValueKind.Array)
            {
                var attachments = new List<AssessmentAttachment>();
                foreach (var attachmentElement in attachmentsElement.EnumerateArray())
                {
                    var attachment = new AssessmentAttachment();
                    if (attachmentElement.TryGetProperty("name", out var attachmentNameElement))
                        attachment.Name = attachmentNameElement.GetString() ?? string.Empty;
                    if (attachmentElement.TryGetProperty("url", out var attachmentUrlElement))
                        attachment.Url = attachmentUrlElement.GetString() ?? string.Empty;
                    if (attachmentElement.TryGetProperty("type", out var attachmentTypeElement))
                        attachment.Type = attachmentTypeElement.GetString() ?? string.Empty;
                    if (attachmentElement.TryGetProperty("size", out var sizeElement))
                        attachment.Size = sizeElement.GetInt64();
                    attachments.Add(attachment);
                }
                assessment.Attachments = attachments;
            }

            return assessment;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
