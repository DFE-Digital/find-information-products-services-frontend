using System.Text.Json;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface IServiceAssessmentsService
{
    Task<(List<AssessmentSummary> Assessments, int TotalCount)> GetAssessmentsSummaryAsync(int page = 1, int pageSize = 25, string? searchQuery = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null);
    Task<Assessment?> GetAssessmentByIdAsync(int id, TimeSpan? cacheDuration = null);
    Task<List<AssessmentSummary>> GetAssessmentsByDocumentIdAsync(string documentId, TimeSpan? cacheDuration = null);
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

    public async Task<List<AssessmentSummary>> GetAssessmentsByDocumentIdAsync(string documentId, TimeSpan? cacheDuration = null)
    {
        var cacheKey = $"assessments_by_documentid_{documentId}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<AssessmentSummary>?>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Retrieved assessments by documentId from cache for key: {CacheKey}", cacheKey);
                return cachedResult;
            }
        }

        try
        {
            // The _baseUrl from config is "https://service-assessments.education.gov.uk/api/product/"
            // So we just need to append the documentId
            var encodedDocumentId = Uri.EscapeDataString(documentId);
            var baseUrl = _baseUrl.TrimEnd('/');
            var url = $"{baseUrl}/{encodedDocumentId}";
            
            _logger.LogInformation("Fetching assessments by documentId from: {Url} for documentId: {DocumentId}. BaseUrl was: {BaseUrl}", 
                url, documentId, _baseUrl);

            var response = await _httpClient.GetAsync(url);
            
            _logger.LogInformation("API response status: {StatusCode} for documentId: {DocumentId}", response.StatusCode, documentId);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("No assessments found for documentId: {DocumentId} (404)", documentId);
                return new List<AssessmentSummary>();
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API call failed with status {StatusCode} for documentId {DocumentId}. Response: {ErrorContent}", 
                    response.StatusCode, documentId, errorContent);
                return new List<AssessmentSummary>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received JSON response length: {Length} for documentId: {DocumentId}. First 500 chars: {Preview}", 
                jsonContent.Length, documentId, jsonContent.Length > 500 ? jsonContent.Substring(0, 500) : jsonContent);
            
            // Try direct deserialization first (for simple JSON format)
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var directAssessments = JsonSerializer.Deserialize<List<AssessmentSummary>>(jsonContent, options);
                if (directAssessments != null && directAssessments.Any())
                {
                    _logger.LogInformation("Successfully deserialized {Count} assessments directly for documentId: {DocumentId}", 
                        directAssessments.Count, documentId);
                    // Cache the result
                    if (cacheDuration.HasValue)
                    {
                        await _cacheService.SetAsync(cacheKey, directAssessments, cacheDuration.Value);
                        _logger.LogDebug("Cached assessments by documentId for key: {CacheKey}", cacheKey);
                    }
                    return directAssessments;
                }
                else
                {
                    _logger.LogWarning("Direct deserialization returned null or empty list for documentId: {DocumentId}", documentId);
                }
            }
            catch (JsonException ex)
            {
                // If direct deserialization fails, try parsing as Strapi format
                _logger.LogDebug("Direct deserialization failed for documentId {DocumentId}: {Error}. Trying Strapi format parsing", 
                    documentId, ex.Message);
            }

            // Fallback to Strapi format parsing
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var assessments = new List<AssessmentSummary>();

            _logger.LogDebug("Parsing JSON as Strapi format. Root element type: {ValueKind}", jsonDocument.RootElement.ValueKind);

            // Handle different response formats
            // Format 1: Direct array
            if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
            {
                _logger.LogDebug("Parsing as direct array format");
                foreach (var item in jsonDocument.RootElement.EnumerateArray())
                {
                    var assessment = ParseAssessmentSummary(item);
                    if (assessment != null)
                    {
                        assessments.Add(assessment);
                    }
                }
            }
            // Format 2: Wrapped in data property
            else if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
            {
                _logger.LogDebug("Parsing as 'data' wrapped format");
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
            // Format 3: Wrapped in assessments property (API response format)
            else if (jsonDocument.RootElement.TryGetProperty("assessments", out var assessmentsElement))
            {
                _logger.LogDebug("Parsing as 'assessments' wrapped format");
                if (assessmentsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in assessmentsElement.EnumerateArray())
                    {
                        // Try API format first (direct properties like AssessmentID, Name, etc.)
                        var assessment = ParseAssessmentSummaryFromApiFormat(item);
                        if (assessment == null)
                        {
                            // Fallback to Strapi format
                            assessment = ParseAssessmentSummary(item);
                        }
                        if (assessment != null)
                        {
                            assessments.Add(assessment);
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Unknown JSON format for documentId {DocumentId}. Root element properties: {Properties}", 
                    documentId, string.Join(", ", jsonDocument.RootElement.EnumerateObject().Select(p => p.Name)));
            }
            
            _logger.LogInformation("Parsed {Count} assessments from Strapi format for documentId: {DocumentId}", 
                assessments.Count, documentId);

            // Cache the result
            if (cacheDuration.HasValue)
            {
                await _cacheService.SetAsync(cacheKey, assessments, cacheDuration.Value);
                _logger.LogDebug("Cached assessments by documentId for key: {CacheKey}", cacheKey);
            }

            return assessments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assessments by documentId from SAS API for documentId: {DocumentId}", documentId);
            return new List<AssessmentSummary>();
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

    private static AssessmentSummary? ParseAssessmentSummaryFromApiFormat(JsonElement element)
    {
        try
        {
            // API format has properties directly on the object (not in "attributes")
            // Properties: AssessmentID, FIPS_ID, Name, Type, Phase, Status, Description, etc.
            
            var assessment = new AssessmentSummary();

            // AssessmentID maps to Id
            if (element.TryGetProperty("AssessmentID", out var assessmentIdElement))
            {
                assessment.Id = assessmentIdElement.GetInt32();
            }

            // FIPS_ID maps to DocumentId
            if (element.TryGetProperty("FIPS_ID", out var fipsIdElement))
            {
                assessment.DocumentId = fipsIdElement.GetString();
            }

            // Name maps to Title
            if (element.TryGetProperty("Name", out var nameElement))
            {
                assessment.Title = nameElement.GetString() ?? string.Empty;
            }
            // Also check for Title (case-insensitive)
            else if (element.TryGetProperty("title", out var titleElement))
            {
                assessment.Title = titleElement.GetString() ?? string.Empty;
            }

            // Type
            if (element.TryGetProperty("Type", out var typeElement))
            {
                assessment.Type = typeElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("type", out var typeElementLower))
            {
                assessment.Type = typeElementLower.GetString() ?? string.Empty;
            }

            // Phase
            if (element.TryGetProperty("Phase", out var phaseElement))
            {
                assessment.Phase = phaseElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("phase", out var phaseElementLower))
            {
                assessment.Phase = phaseElementLower.GetString() ?? string.Empty;
            }

            // Status
            if (element.TryGetProperty("Status", out var statusElement))
            {
                assessment.Status = statusElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("status", out var statusElementLower))
            {
                assessment.Status = statusElementLower.GetString() ?? string.Empty;
            }

            // Outcome
            if (element.TryGetProperty("Outcome", out var outcomeElement))
            {
                assessment.Outcome = outcomeElement.GetString();
            }
            else if (element.TryGetProperty("outcome", out var outcomeElementLower))
            {
                assessment.Outcome = outcomeElementLower.GetString();
            }

            // Description
            if (element.TryGetProperty("Description", out var descriptionElement))
            {
                assessment.Description = descriptionElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("description", out var descriptionElementLower))
            {
                assessment.Description = descriptionElementLower.GetString() ?? string.Empty;
            }

            // StartDate
            if (element.TryGetProperty("StartDate", out var startDateElement) && startDateElement.ValueKind != JsonValueKind.Null)
            {
                if (startDateElement.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(startDateElement.GetString(), out var startDate))
                    {
                        assessment.StartDate = startDate;
                    }
                }
            }
            else if (element.TryGetProperty("startDate", out var startDateElementLower) && startDateElementLower.ValueKind != JsonValueKind.Null)
            {
                if (startDateElementLower.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(startDateElementLower.GetString(), out var startDate))
                    {
                        assessment.StartDate = startDate;
                    }
                }
            }

            // EndDate
            if (element.TryGetProperty("EndDate", out var endDateElement) && endDateElement.ValueKind != JsonValueKind.Null)
            {
                if (endDateElement.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(endDateElement.GetString(), out var endDate))
                    {
                        assessment.EndDate = endDate;
                    }
                }
            }
            else if (element.TryGetProperty("endDate", out var endDateElementLower) && endDateElementLower.ValueKind != JsonValueKind.Null)
            {
                if (endDateElementLower.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(endDateElementLower.GetString(), out var endDate))
                    {
                        assessment.EndDate = endDate;
                    }
                }
            }

            // URL
            if (element.TryGetProperty("Url", out var urlElement))
            {
                assessment.Url = urlElement.GetString();
            }
            else if (element.TryGetProperty("url", out var urlElementLower))
            {
                assessment.Url = urlElementLower.GetString();
            }

            // Only return if we have at least an ID or Title
            if (assessment.Id > 0 || !string.IsNullOrEmpty(assessment.Title))
            {
                return assessment;
            }

            return null;
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
