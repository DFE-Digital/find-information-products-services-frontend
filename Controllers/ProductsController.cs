using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FipsFrontend.Services;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class ProductsController : Controller
{
    private readonly ILogger<ProductsController> _logger;
    private readonly CmsApiService _cmsApiService;
    private readonly IOptimizedCmsApiService _optimizedCmsApiService;
    private readonly EnabledFeatures _enabledFeatures;
    private readonly IConfiguration _configuration;
    private readonly INotifyService _notifyService;
    private readonly ISearchTermLoggingService _searchTermLoggingService;
    private readonly IServiceAssessmentsService _assessmentsService;

    public ProductsController(ILogger<ProductsController> logger, CmsApiService cmsApiService, IOptimizedCmsApiService optimizedCmsApiService, IOptions<EnabledFeatures> enabledFeatures, IConfiguration configuration, INotifyService notifyService, ISearchTermLoggingService searchTermLoggingService, IServiceAssessmentsService assessmentsService)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _optimizedCmsApiService = optimizedCmsApiService;
        _enabledFeatures = enabledFeatures.Value;
        _configuration = configuration;
        _notifyService = notifyService;
        _searchTermLoggingService = searchTermLoggingService;
        _assessmentsService = assessmentsService;
    }

    // GET: Products
    public async Task<IActionResult> Index(string? keywords, string[]? phase, string[]? group, string[]? subgroup,
        string[]? channel, string[]? type, string[]? cmdbStatus, string[]? parent, int page = 1, string? searchSource = null)
    {
        try
        {
            var viewModel = new ProductsViewModel();

            // Normalise and parse the raw keywords into a list of terms
            var rawKeywords = keywords;
            var parsedKeywords = ParseKeywords(rawKeywords);

            // Track whether this search originated from a user group category view
            viewModel.IsUserGroupSearch = string.Equals(searchSource, "userGroup", StringComparison.OrdinalIgnoreCase);

            // Get cache durations from configuration
            var categoryTypesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryTypes", 15));
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));

            // Debug: Get all category types to see what's available
            var allCategoryTypes = await _optimizedCmsApiService.GetAllCategoryTypes(categoryTypesDuration);
            _logger.LogInformation("Available category types: {Types}", 
                string.Join(", ", allCategoryTypes?.Select(ct => ct.Name) ?? new List<string>()));

            // Load category values for filters using individual API calls for each category type
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();

            // Debug logging for category values
            _logger.LogInformation("Category values loaded: Phase={PhaseCount}, Channel={ChannelCount}, BusinessArea={BusinessAreaCount}", 
                phaseValues.Count, channelValues.Count, groupValues.Count);
            
            // Additional debug logging for Business area values
            if (groupValues.Any())
            {
                _logger.LogInformation("Business area values: {Values}", 
                    string.Join(", ", groupValues.Take(10).Select(v => $"{v.Name} ({v.Slug})")));
            }
            else
            {
                _logger.LogWarning("No Business area values loaded - check CMS category type name");
            }

            // Build CMS filter query string
            var filterQuery = BuildCmsFilterQuery(phase, group, subgroup, channel, type, cmdbStatus, parent);

            // Get filtered products with count in a single call (cache for 5 minutes)
            var pageSize = 25;
            var cmsPage = page;
            List<Product> filteredProducts;
            int filteredTotalCount;
            int totalCount;

            // Build server-side filters for CMS API
            var serverFilters = new Dictionary<string, string[]>();
            
            // Add state filter (default to Active for public products page)
            serverFilters["state"] = new[] { "Active" };
            
            // Track which filters have "not categorised" selected for client-side filtering
            var hasNotCategorisedPhase = phase?.Contains("__not_categorised__", StringComparer.OrdinalIgnoreCase) == true;
            var hasNotCategorisedChannel = channel?.Contains("__not_categorised__", StringComparer.OrdinalIgnoreCase) == true;
            var hasNotCategorisedType = type?.Contains("__not_categorised__", StringComparer.OrdinalIgnoreCase) == true;
            var hasNotCategorisedGroup = group?.Contains("__not_categorised__", StringComparer.OrdinalIgnoreCase) == true;
            
            // Convert category filters to server-side format, excluding "__not_categorised__" values
            var phaseFilters = phase?.Where(p => !p.Equals("__not_categorised__", StringComparison.OrdinalIgnoreCase)).ToArray();
            var channelFilters = channel?.Where(c => !c.Equals("__not_categorised__", StringComparison.OrdinalIgnoreCase)).ToArray();
            var typeFilters = type?.Where(t => !t.Equals("__not_categorised__", StringComparison.OrdinalIgnoreCase)).ToArray();
            var groupFilters = group?.Where(g => !g.Equals("__not_categorised__", StringComparison.OrdinalIgnoreCase)).ToArray();
            
            // Count how many filter categories are active
            int activeCategoryCount = 0;
            if (phaseFilters?.Length > 0) activeCategoryCount++;
            if (channelFilters?.Length > 0) activeCategoryCount++;
            if (typeFilters?.Length > 0) activeCategoryCount++;
            if (groupFilters?.Length > 0 || subgroup?.Length > 0) activeCategoryCount++;
            if (parent?.Length > 0) activeCategoryCount++;

            // Use optimized service for search with caching and server-side filtering
            var searchDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Search", 2));
            var productsDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Products", 15));
            
            var cacheDuration = parsedKeywords.Any() ? searchDuration : productsDuration;
            
            // Use client-side filtering if:
            // 1. "not categorised" filters are active (need to check for absence of values)
            // 2. Multiple filter categories are active (need AND logic between categories, OR within)
            if (hasNotCategorisedPhase || hasNotCategorisedChannel || hasNotCategorisedType || hasNotCategorisedGroup || activeCategoryCount > 1)
            {
                // Build filters excluding category types we're checking for "not categorised"
                var filtersForNotCategorised = new Dictionary<string, string[]>();
                filtersForNotCategorised["state"] = new[] { "Active" };
                
                // Only include category filters for types we're NOT checking "not categorised" for
                var categorySlugFiltersForQuery = new List<string>();
                
                if (!hasNotCategorisedPhase && phaseFilters?.Length > 0)
                {
                    categorySlugFiltersForQuery.AddRange(phaseFilters);
                }
                if (!hasNotCategorisedChannel && channelFilters?.Length > 0)
                {
                    categorySlugFiltersForQuery.AddRange(channelFilters);
                }
                if (!hasNotCategorisedType && typeFilters?.Length > 0)
                {
                    categorySlugFiltersForQuery.AddRange(typeFilters);
                }
                if (!hasNotCategorisedGroup && groupFilters?.Length > 0)
                {
                    categorySlugFiltersForQuery.AddRange(groupFilters);
                }
                if (subgroup?.Length > 0)
                {
                    categorySlugFiltersForQuery.AddRange(subgroup);
                }
                if (parent?.Length > 0)
                {
                    categorySlugFiltersForQuery.AddRange(parent);
                }
                
                if (categorySlugFiltersForQuery.Count > 0)
                {
                    filtersForNotCategorised["category_values.slug"] = categorySlugFiltersForQuery.ToArray();
                }
                
                // Get ALL products matching other filters (not paginated yet) with full details
                // Use a large page size to get all products in one or few requests
                var largePageSize = 1000;
                var allProductsResult = await _optimizedCmsApiService.GetProductsForListingAsync2(1, largePageSize, parsedKeywords, filtersForNotCategorised, cacheDuration);
                var allProducts = allProductsResult.Products;
                
                // If there are more pages, fetch them
                var totalPagesNeeded = (int)Math.Ceiling((double)allProductsResult.TotalCount / largePageSize);
                if (totalPagesNeeded > 1)
                {
                    var additionalProducts = new List<Product>();
                    for (int pageNum = 2; pageNum <= totalPagesNeeded; pageNum++)
                    {
                        var pageResult = await _optimizedCmsApiService.GetProductsForListingAsync2(pageNum, largePageSize, parsedKeywords, filtersForNotCategorised, cacheDuration);
                        additionalProducts.AddRange(pageResult.Products);
                    }
                    allProducts = allProducts.Concat(additionalProducts).ToList();
                }
                
                // Note: Keyword and category filters are already applied by GetProductsForListingAsync2
                // We only need to apply the "not categorised" filters client-side now
                
                // Now apply "not categorised" filters with OR logic
                filteredProducts = allProducts.Where(p =>
                {
                    // Check Phase filter with OR logic
                    if (hasNotCategorisedPhase || (phaseFilters?.Length > 0))
                    {
                        // Check if product has ANY Phase category value
                        var hasPhase = p.CategoryValues?.Any(cv => 
                            cv.CategoryType != null && 
                            cv.CategoryType.Name.Equals("Phase", StringComparison.OrdinalIgnoreCase)) == true;
                        
                        // Check if product matches regular Phase filters
                        var matchesPhaseFilter = false;
                        if (phaseFilters?.Length > 0)
                        {
                            matchesPhaseFilter = p.CategoryValues?.Any(cv => 
                                cv.CategoryType != null &&
                                cv.CategoryType.Name.Equals("Phase", StringComparison.OrdinalIgnoreCase) &&
                                phaseFilters.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)) == true;
                        }
                        
                        // OR logic: match regular filter OR (not categorised selected AND no phase)
                        var phaseMatch = matchesPhaseFilter || (hasNotCategorisedPhase && !hasPhase);
                        if (!phaseMatch) return false;
                    }
                    
                    // Check Channel filter with OR logic
                    if (hasNotCategorisedChannel || (channelFilters?.Length > 0))
                    {
                        var hasChannel = p.CategoryValues?.Any(cv => 
                            cv.CategoryType != null && 
                            cv.CategoryType.Name.Equals("Channel", StringComparison.OrdinalIgnoreCase)) == true;
                        
                        var matchesChannelFilter = false;
                        if (channelFilters?.Length > 0)
                        {
                            matchesChannelFilter = p.CategoryValues?.Any(cv => 
                                cv.CategoryType != null &&
                                cv.CategoryType.Name.Equals("Channel", StringComparison.OrdinalIgnoreCase) &&
                                channelFilters.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)) == true;
                        }
                        
                        var channelMatch = matchesChannelFilter || (hasNotCategorisedChannel && !hasChannel);
                        if (!channelMatch) return false;
                    }
                    
                    // Check Type filter with OR logic
                    if (hasNotCategorisedType || (typeFilters?.Length > 0))
                    {
                        var hasType = p.CategoryValues?.Any(cv => 
                            cv.CategoryType != null && 
                            cv.CategoryType.Name.Equals("Type", StringComparison.OrdinalIgnoreCase)) == true;
                        
                        var matchesTypeFilter = false;
                        if (typeFilters?.Length > 0)
                        {
                            matchesTypeFilter = p.CategoryValues?.Any(cv => 
                                cv.CategoryType != null &&
                                cv.CategoryType.Name.Equals("Type", StringComparison.OrdinalIgnoreCase) &&
                                typeFilters.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)) == true;
                        }
                        
                        var typeMatch = matchesTypeFilter || (hasNotCategorisedType && !hasType);
                        if (!typeMatch) return false;
                    }
                    
                    // Check Business area filter with OR logic
                    if (hasNotCategorisedGroup || (groupFilters?.Length > 0))
                    {
                        var hasBusinessArea = p.CategoryValues?.Any(cv => 
                            cv.CategoryType != null && 
                            cv.CategoryType.Name.Equals("Business area", StringComparison.OrdinalIgnoreCase)) == true;
                        
                        var matchesGroupFilter = false;
                        if (groupFilters?.Length > 0)
                        {
                            matchesGroupFilter = p.CategoryValues?.Any(cv => 
                                cv.CategoryType != null &&
                                cv.CategoryType.Name.Equals("Business area", StringComparison.OrdinalIgnoreCase) &&
                                groupFilters.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)) == true;
                        }
                        
                        var groupMatch = matchesGroupFilter || (hasNotCategorisedGroup && !hasBusinessArea);
                        if (!groupMatch) return false;
                    }
                    
                    return true;
                }).ToList();
                
                // Calculate total count before pagination
                filteredTotalCount = filteredProducts.Count;
                totalCount = filteredProducts.Count;
                
                // Apply pagination after filtering
                filteredProducts = filteredProducts
                    .Skip((cmsPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                // Single category filter or no category filters - can use server-side filtering
                // Add the single active category filter to serverFilters
                if (phaseFilters?.Length > 0)
                {
                    serverFilters["category_values.slug"] = phaseFilters;
                }
                else if (channelFilters?.Length > 0)
                {
                    serverFilters["category_values.slug"] = channelFilters;
                }
                else if (typeFilters?.Length > 0)
                {
                    serverFilters["category_values.slug"] = typeFilters;
                }
                else if (groupFilters?.Length > 0 || subgroup?.Length > 0)
                {
                    var businessAreaFilters = new List<string>();
                    if (groupFilters?.Length > 0) businessAreaFilters.AddRange(groupFilters);
                    if (subgroup?.Length > 0) businessAreaFilters.AddRange(subgroup);
                    serverFilters["category_values.slug"] = businessAreaFilters.ToArray();
                }
                else if (parent?.Length > 0)
                {
                    serverFilters["category_values.slug"] = parent;
                }
                
                var result = await _optimizedCmsApiService.GetProductsForListingAsync2(cmsPage, pageSize, parsedKeywords, serverFilters, cacheDuration);
                filteredProducts = result.Products;
                filteredTotalCount = result.TotalCount;
                totalCount = result.TotalCount;
            }

            // Ensure view model properties are initialized
            viewModel.PhaseOptions = new List<FilterOption>();
            viewModel.GroupOptions = new List<FilterOption>();
            viewModel.ChannelOptions = new List<FilterOption>();
            viewModel.TypeOptions = new List<FilterOption>();
            viewModel.CmdbStatusOptions = new List<FilterOption>();
            viewModel.CmdbGroupOptions = new List<FilterOption>();
            viewModel.SelectedFilters = new List<SelectedFilter>();

            // Set up view model
            viewModel.TotalCount = totalCount;
            viewModel.FilteredCount = filteredTotalCount;

            // Set pagination properties
            viewModel.CurrentPage = Math.Max(1, page);
            viewModel.PageSize = pageSize;

            // Products are already paginated by CMS, so use them directly
            viewModel.Products = filteredProducts;


            // Set filter values
            viewModel.Keywords = rawKeywords;
            viewModel.KeywordTerms = parsedKeywords;
            viewModel.SelectedPhases = phase?.ToList() ?? new List<string>();
            viewModel.SelectedGroups = group?.ToList() ?? new List<string>();
            viewModel.SelectedSubgroups = subgroup?.ToList() ?? new List<string>();
            viewModel.SelectedChannels = channel?.ToList() ?? new List<string>();
            viewModel.SelectedTypes = type?.ToList() ?? new List<string>();
            viewModel.SelectedCmdbStatuses = cmdbStatus?.ToList() ?? new List<string>();
            viewModel.SelectedCmdbGroups = parent?.ToList() ?? new List<string>();

            // Build filter options - get all products for counting, not just the current page
            var allProductsForCounting = await _optimizedCmsApiService.GetProductsForFilterCountsAsync(productsDuration, "Active");
            await BuildFilterOptions(viewModel, allProductsForCounting, phaseValues, channelValues, groupValues);

            // Build selected filters for display
            BuildSelectedFilters(viewModel);

            // Log search term (non-blocking, with rate limiting and deduplication)
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();
                // Extract product document IDs and titles from results
                var searchResults = filteredProducts?
                    .Where(p => !string.IsNullOrEmpty(p.DocumentId))
                    .Select(p => new SearchResult
                    {
                        DocumentId = p.DocumentId ?? string.Empty,
                        Title = p.Title ?? string.Empty
                    })
                    .ToList();
                _searchTermLoggingService.LogSearchTerm(keywords, filteredTotalCount, searchResults, ipAddress, userAgent);
            }

            ViewData["ActiveNav"] = "products";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products");
            return View(new ProductsViewModel());
        }
    }

    // GET: Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            var product = await _optimizedCmsApiService.GetProductByIdAsync(id, productDetailDuration);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product details for ID: {Id}", id);
            return NotFound();
        }
    }

    // GET: Products/View/{fipsid} or Products/View?ref={documentId}
    // Supports both route parameter and query parameter 'ref' for documentId
    public async Task<IActionResult> ViewProduct(string fipsid, [FromQuery(Name = "ref")] string? refParam, [FromQuery] string? returnUrl)
    {
        // Use 'ref' query parameter if provided, otherwise use route parameter
        var productId = refParam ?? fipsid;

        Console.WriteLine("productId: " + productId);

        // Check if productId is null or empty
        if (string.IsNullOrEmpty(productId))
        {
            return NotFound();
        }

        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            // Find product by fips_id or documentId - optimized to return only needed fields
            // GetProductByFipsIdAsync can handle both FipsId and DocumentId
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(productId, productDetailDuration);

            if (product == null)
            {
                return NotFound();
            }

            // Capture return URL - either from query parameter or from referrer
            if (string.IsNullOrEmpty(returnUrl))
            {
                var referrer = Request.Headers["Referer"].FirstOrDefault();
                if (!string.IsNullOrEmpty(referrer) && 
                    referrer.Contains("/products") && 
                    (referrer.Contains("?") || referrer.Contains("&")))
                {
                    returnUrl = referrer;
                }
            }

            var viewModel = new ProductViewModel
            {
                Product = product,
                PageTitle = product.Title,
                PageDescription = $"View detailed information about {product.Title}"
            };
            ViewData["ActiveNav"] = "products";
            ViewData["AssuranceEnabled"] = _enabledFeatures.Assurance;
            ViewData["EditProductEnabled"] = _enabledFeatures.EditProduct;
            ViewData["ReturnUrl"] = returnUrl; // Pass returnUrl to the view
            return View("~/Views/Product/index.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product details for Product ID: {ProductId}", productId);
            return NotFound();
        }
    }

    public async Task<IActionResult> ProductCategories(string fipsid, [FromQuery] string? returnUrl)
    {
        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            // Find product by fips_id - optimized for category view
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);

            if (product == null)
            {
                return NotFound();
            }

            // Build category information
            var categoryInfo = new List<ProductCategoryInfo>();

            if (product.CategoryValues?.Any() == true)
            {
                _logger.LogInformation("Product has {Count} category values assigned", product.CategoryValues.Count);
                
                // Log all category values for debugging
                foreach (var cv in product.CategoryValues)
                {
                    _logger.LogInformation("Category Value: Name={Name}, Slug={Slug}, CategoryType={Type}", 
                        cv.Name, cv.Slug, cv.CategoryType?.Name ?? "NULL");
                }
                
                var groupedCategories = product.CategoryValues
                    .GroupBy(cv => cv.CategoryType?.Name ?? "Unknown")
                    .OrderBy(g => g.Key);

                foreach (var group in groupedCategories)
                {
                    var categoryType = group.First().CategoryType;
                    _logger.LogInformation("Processing group: {GroupKey}, CategoryType is null: {IsNull}, Count: {Count}", 
                        group.Key, categoryType == null, group.Count());
                    
                    if (categoryType != null)
                    {
                        var info = new ProductCategoryInfo
                        {
                            CategoryTypeName = categoryType.Name,
                            CategoryTypeSlug = categoryType.Slug,
                            IsMultiLevel = categoryType.MultiLevel,
                            CategoryValueNames = group.Select(cv => cv.Name).ToList(),
                            CategoryValueSlugs = group.Select(cv => cv.Slug).ToList(),
                            CategoryValueDescriptions = group.Select(cv => cv.ShortDescription ?? string.Empty).ToList(),
                            CategoryValueDocumentIds = group.Select(cv => cv.DocumentId ?? string.Empty).ToList(),
                            CategoryValueSearchTexts = group.Select(cv => cv.SearchText ?? string.Empty).ToList()
                        };
                        categoryInfo.Add(info);
                        _logger.LogInformation("Added category group: {Name} with {Count} values", categoryType.Name, info.CategoryValueNames.Count);
                        
                        // Log search_text values for debugging - check both the model and the mapped list
                        foreach (var cv in group)
                        {
                            _logger.LogInformation("  Category value '{Name}' (DocumentId: {DocId}): cv.SearchText = '{SearchText}' (null: {IsNull})", 
                                cv.Name, cv.DocumentId ?? "null", cv.SearchText ?? "(null)", cv.SearchText == null);
                        }
                        
                        for (int i = 0; i < info.CategoryValueNames.Count; i++)
                        {
                            var searchText = i < info.CategoryValueSearchTexts.Count ? info.CategoryValueSearchTexts[i] : "(not found)";
                            _logger.LogInformation("  Mapped Category value '{Name}': search_text = '{SearchText}'", 
                                info.CategoryValueNames[i], searchText ?? "(null)");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Skipping category group '{GroupKey}' because CategoryType is null. Values in group: {Values}", 
                            group.Key, string.Join(", ", group.Select(cv => cv.Name)));
                    }
                }
                
                _logger.LogInformation("Final category info count: {Count}", categoryInfo.Count);
            }

            // No need to fetch search terms - we just display the search_text from category values

            var viewModel = new ProductCategoriesViewModel
            {
                Product = product,
                CategoryInfo = categoryInfo,
                SearchTerms = new List<SearchTerm>(), // Not used anymore - search terms are now per category value
                PageTitle = $"{product.Title} - Categories",
                PageDescription = $"View all categories and values assigned to {product.Title}"
            };

            ViewData["ActiveNav"] = "products";
            ViewData["AssuranceEnabled"] = _enabledFeatures.Assurance;
            ViewData["EditProductEnabled"] = _enabledFeatures.EditProduct;
            ViewData["ReturnUrl"] = returnUrl; // Pass returnUrl to the view
            return View("~/Views/Product/categories.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product categories for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }

    public async Task<IActionResult> ProductAssurance(string fipsid, [FromQuery] string? returnUrl)
    {
        // Check if Assurance feature is enabled
        if (!_enabledFeatures.Assurance)
        {
            return NotFound();
        }

        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            var assessmentsDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Assessments", 15));
            
            // Find product by fips_id with product assurances populated - optimized for assurance view
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);

            if (product == null)
            {
                return NotFound();
            }

            // Start with any product assurances already stored in CMS
            var combinedAssurances = product.ProductAssurances != null
                ? new List<ProductAssurance>(product.ProductAssurances)
                : new List<ProductAssurance>();

            // Hydrate additional assessments directly from Service Assessment Service by product DocumentId
            if (!string.IsNullOrEmpty(product.DocumentId))
            {
                try
                {
                    var assessments = await _assessmentsService.GetAssessmentsByDocumentIdAsync(
                        product.DocumentId,
                        cacheDuration: assessmentsDuration
                    );

                    if (assessments.Any())
                    {
                        // Avoid duplicates using ExternalUrl / Url as the key where available
                        var existingUrls = new HashSet<string>(
                            combinedAssurances
                                .Where(a => !string.IsNullOrEmpty(a.ExternalUrl))
                                .Select(a => a.ExternalUrl!),
                            StringComparer.OrdinalIgnoreCase
                        );

                        foreach (var assessment in assessments)
                        {
                            if (!string.IsNullOrEmpty(assessment.Url) && existingUrls.Contains(assessment.Url))
                            {
                                continue;
                            }

                            var assurance = new ProductAssurance
                            {
                                DocumentId = assessment.DocumentId,
                                AssuranceType = assessment.Type,
                                ExternalUrl = assessment.Url,
                                DateOfAssurance = assessment.EndDate ?? assessment.StartDate,
                                Outcome = !string.IsNullOrEmpty(assessment.Outcome) ? assessment.Outcome : assessment.Status,
                                Phase = assessment.Phase
                            };

                            combinedAssurances.Add(assurance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load additional assessments from Service Assessment Service for product documentId {DocumentId}", product.DocumentId);
                }
            }

            var viewModel = new ProductAssuranceViewModel
            {
                Product = product,
                ProductAssurances = combinedAssurances
                    .OrderByDescending(a => a.DateOfAssurance ?? DateTime.MinValue)
                    .ToList(),
                PageTitle = $"{product.Title} - Assurance",
                PageDescription = $"View assurance information for {product.Title}"
            };

            ViewData["ActiveNav"] = "products";
            ViewData["AssuranceEnabled"] = _enabledFeatures.Assurance;
            ViewData["EditProductEnabled"] = _enabledFeatures.EditProduct;
            ViewData["ReturnUrl"] = returnUrl; // Pass returnUrl to the view
            return View("~/Views/Product/assurance.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product assurance for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }
    private List<Product> ApplyFilters(List<Product> products, string? keywords, string[]? phase, string[]? group,
        string[]? subgroup, string[]? channel, string[]? type, string[]? cmdbStatus, string[]? parent)
    {
        var filtered = products.AsEnumerable();

        // Note: Keyword search is now handled by CMS API call, not here

        // Apply phase filter (based on category values with category type "Phase")
        if (phase?.Length > 0)
        {
            filtered = filtered.Where(p =>
                p.CategoryValues?.Any(cv =>
                    cv.CategoryType?.Name.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true &&
                    phase.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)
                ) == true);
        }

        // Apply channel filter (based on category values with category type "Channel")
        if (channel?.Length > 0)
        {
            filtered = filtered.Where(p =>
                p.CategoryValues?.Any(cv =>
                    cv.CategoryType?.Name.Equals("Channel", StringComparison.OrdinalIgnoreCase) == true &&
                    channel.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)
                ) == true);
        }

        // Apply type filter (based on category values with category type "Type")
        if (type?.Length > 0)
        {
            filtered = filtered.Where(p =>
                p.CategoryValues?.Any(cv =>
                    cv.CategoryType?.Name.Equals("Type", StringComparison.OrdinalIgnoreCase) == true &&
                    type.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)
                ) == true);
        }

        // Apply CMDB status filter (also based on state for now)
        if (cmdbStatus?.Length > 0)
        {
            filtered = filtered.Where(p => cmdbStatus.Contains(p.State, StringComparer.OrdinalIgnoreCase));
        }

        // Apply group/subgroup filters (based on category values with category type "Business area")
        if (group?.Length > 0 || subgroup?.Length > 0)
        {
            filtered = filtered.Where(p =>
                p.CategoryValues?.Any(cv =>
                    cv.CategoryType?.Name.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true &&
                    ((group?.Length > 0 && group.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)) ||
                     (subgroup?.Length > 0 && subgroup.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)))
                ) == true);
        }

        // Apply CMDB group filter (based on category values)
        if (parent?.Length > 0)
        {
            filtered = filtered.Where(p =>
                p.CategoryValues?.Any(cv =>
                    parent.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)
                ) == true);
        }

        return filtered.ToList();
    }

    private string BuildCmsFilterQuery(string[]? phase, string[]? group, string[]? subgroup, string[]? channel,
        string[]? type, string[]? cmdbStatus, string[]? parent)
    {
        var filters = new List<string>();

        // Apply phase filter (based on category values with category type "Phase")
        if (phase?.Length > 0)
        {
            foreach (var p in phase)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(p)}");
                }
            }
        }

        // Apply channel filter (based on category values with category type "Channel")
        if (channel?.Length > 0)
        {
            foreach (var c in channel)
            {
                if (!string.IsNullOrEmpty(c))
                {
                    filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(c)}");
                }
            }
        }

        // Apply type filter (based on category values with category type "Type")
        if (type?.Length > 0)
        {
            foreach (var t in type)
            {
                if (!string.IsNullOrEmpty(t))
                {
                    filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(t)}");
                }
            }
        }

        // Apply CMDB status filter (based on state)
        if (cmdbStatus?.Length > 0)
        {
            foreach (var s in cmdbStatus)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    filters.Add($"filters[state][$eq]={Uri.EscapeDataString(s)}");
                }
            }
        }

        // Apply group/subgroup filters (based on category values with category type "Group")
        if (group?.Length > 0 || subgroup?.Length > 0)
        {
            var allGroupSlugs = new List<string>();
            if (group?.Length > 0) allGroupSlugs.AddRange(group.Where(g => !string.IsNullOrEmpty(g)));
            if (subgroup?.Length > 0) allGroupSlugs.AddRange(subgroup.Where(sg => !string.IsNullOrEmpty(sg)));

            foreach (var g in allGroupSlugs)
            {
                filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(g)}");
            }
        }

        // Apply CMDB group filter (based on category values)
        if (parent?.Length > 0)
        {
            foreach (var p in parent)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(p)}");
                }
            }
        }

        return string.Join("&", filters);
    }

    private async Task BuildFilterOptions(ProductsViewModel viewModel, List<Product> allProductsForCounting,
        List<CategoryValue> phaseValues, List<CategoryValue> channelValues,
        List<CategoryValue> groupValues)
    {
        _logger.LogInformation("Building filter options with provided products for counting");
        _logger.LogInformation("Loaded {Count} products for filter counting", allProductsForCounting.Count);

        // Build phase options from specific phase values
        if (phaseValues.Any())
        {
            _logger.LogInformation("Found {Count} phase values to process", phaseValues.Count);

            viewModel.PhaseOptions = new List<FilterOption>();
            foreach (var pv in phaseValues)
            {
                // Count products with this phase value locally (much faster)
                var actualCount = allProductsForCounting.Count(p => p.CategoryValues?.Any(cv =>
                    cv.Slug.Equals(pv.Slug, StringComparison.OrdinalIgnoreCase)) == true);

                _logger.LogInformation("Phase '{Name}' (Slug: {Slug}): {Count} products", pv.Name, pv.Slug, actualCount);

                viewModel.PhaseOptions.Add(new FilterOption
                {
                    Value = pv.Slug,
                    Text = pv.Name,
                    Count = actualCount,
                    IsSelected = viewModel.SelectedPhases.Contains(pv.Slug, StringComparer.OrdinalIgnoreCase)
                });
            }
        }

        // Build channel options from specific channel values
        if (channelValues.Any())
        {
            _logger.LogInformation("Channel values: {Count} items", channelValues.Count);

            viewModel.ChannelOptions = new List<FilterOption>();
            foreach (var cv in channelValues)
            {
                // Count products with this channel value locally (much faster)
                var actualCount = allProductsForCounting.Count(p => p.CategoryValues?.Any(categoryValue =>
                    categoryValue.Slug.Equals(cv.Slug, StringComparison.OrdinalIgnoreCase)) == true);

                _logger.LogInformation("Channel '{Name}' (Slug: {Slug}): {Count} products", cv.Name, cv.Slug, actualCount);

                viewModel.ChannelOptions.Add(new FilterOption
                {
                    Value = cv.Slug,
                    Text = cv.Name,
                    Count = actualCount,
                    IsSelected = viewModel.SelectedChannels.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)
                });
            }
        }

        // Build type options from category type "Type" using individual API call
        var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15))) ?? new List<CategoryValue>();
        
        if (typeValues.Any())
        {
            _logger.LogInformation("Found {Count} type values to process", typeValues.Count);

            viewModel.TypeOptions = new List<FilterOption>();
            foreach (var tv in typeValues)
            {
                // Count products with this type value locally (much faster)
                var actualCount = allProductsForCounting.Count(p => p.CategoryValues?.Any(categoryValue =>
                    categoryValue.Slug.Equals(tv.Slug, StringComparison.OrdinalIgnoreCase)) == true);

                _logger.LogInformation("Type '{Name}' (Slug: {Slug}): {Count} products", tv.Name, tv.Slug, actualCount);

                viewModel.TypeOptions.Add(new FilterOption
                {
                    Value = tv.Slug,
                    Text = tv.Name,
                    Count = actualCount,
                    IsSelected = viewModel.SelectedTypes.Contains(tv.Slug, StringComparer.OrdinalIgnoreCase)
                });
            }
        }

        // Build CMDB status options (based on product states for now)
        var stateGroups = allProductsForCounting.GroupBy(p => p.State).OrderBy(g => g.Key);
        viewModel.CmdbStatusOptions = stateGroups.Select(g => new FilterOption
        {
            Value = g.Key,
            Text = g.Key,
            Count = g.Count(),
            IsSelected = viewModel.SelectedCmdbStatuses.Contains(g.Key, StringComparer.OrdinalIgnoreCase)
        }).ToList();

        // Build group options from specific group values - only process root-level groups (those without parents)
        if (groupValues.Any())
        {
            _logger.LogInformation("Found {Count} group values to process", groupValues.Count);
            
            // Filter to only root-level groups (those without parents)
            var rootGroups = groupValues.Where(gv => gv.Parent == null && gv.Enabled).ToList();
            _logger.LogInformation("Found {Count} root-level groups to process", rootGroups.Count);

            viewModel.GroupOptions = rootGroups.Select(gv =>
            {
                var option = new FilterOption
                {
                    Value = gv.Slug,
                    Text = gv.Name,
                    Count = CountProductsWithCategoryValue(allProductsForCounting, gv.Slug),
                    IsSelected = viewModel.SelectedGroups.Contains(gv.Slug, StringComparer.OrdinalIgnoreCase)
                };

                // Add subgroups - only direct children from the populated relationship
                if (gv.Children?.Any() == true)
                {
                    var enabledChildren = gv.Children.Where(c => c.Enabled).ToList();
                    
                    if (enabledChildren.Any())
                    {
                        option.SubOptions = enabledChildren
                            .OrderBy(c => c.SortOrder ?? 0)
                            .ThenBy(c => c.Name)
                            .Select(child => new FilterOption
                            {
                                Value = child.Slug,
                                Text = child.Name,
                                Count = CountProductsWithCategoryValue(allProductsForCounting, child.Slug),
                                IsSelected = viewModel.SelectedSubgroups.Contains(child.Slug, StringComparer.OrdinalIgnoreCase)
                            }).ToList();
                            
                        _logger.LogInformation("Added {Count} sub-options for group '{GroupName}'", option.SubOptions.Count, gv.Name);
                    }
                }

                return option;
            }).ToList();
        }

        // CMDB group options removed - not needed for the specified category types
    }

    private int CountProductsWithCategoryValue(List<Product> products, string categoryValueSlug)
    {
        return products.Count(p => p.CategoryValues?.Any(cv =>
            cv.Slug.Equals(categoryValueSlug, StringComparison.OrdinalIgnoreCase)) == true);
    }

    private void BuildSelectedFilters(ProductsViewModel viewModel)
    {
        var selectedFilters = new List<SelectedFilter>();

        // Add search term (keywords) filters
        if (viewModel.KeywordTerms != null && viewModel.KeywordTerms.Any())
        {
            foreach (var term in viewModel.KeywordTerms)
            {
                selectedFilters.Add(new SelectedFilter
                {
                    Category = viewModel.IsUserGroupSearch ? "User group" : "Search term",
                    Value = term,
                    DisplayText = term,
                    RemoveUrl = BuildRemoveFilterUrl(viewModel, "keywords", term)
                });
            }
        }
        else if (!string.IsNullOrWhiteSpace(viewModel.Keywords))
        {
            // Fallback: single badge using raw keywords if parsing did not yield terms
            selectedFilters.Add(new SelectedFilter
            {
                Category = viewModel.IsUserGroupSearch ? "User group" : "Search term",
                Value = viewModel.Keywords,
                DisplayText = viewModel.Keywords,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "keywords", viewModel.Keywords)
            });
        }

        // Add phase filters
        foreach (var phase in viewModel.SelectedPhases)
        {
            string displayText;
            if (phase.Equals("__not_categorised__", StringComparison.OrdinalIgnoreCase))
            {
                displayText = "Not categorised";
            }
            else
            {
                var phaseOption = viewModel.PhaseOptions.FirstOrDefault(p => p.Value.Equals(phase, StringComparison.OrdinalIgnoreCase));
                displayText = phaseOption?.Text ?? phase; // Use option text or fallback to slug

                // Remove count from display text if present
                if (displayText.Contains(" ("))
                {
                    var lastParenIndex = displayText.LastIndexOf(" (");
                    if (lastParenIndex > 0)
                    {
                        displayText = displayText.Substring(0, lastParenIndex);
                    }
                }
            }

            selectedFilters.Add(new SelectedFilter
            {
                Category = "Phase",
                Value = phase,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "phase", phase)
            });
        }

        // Add channel filters
        foreach (var channel in viewModel.SelectedChannels)
        {
            string displayText;
            if (channel.Equals("__not_categorised__", StringComparison.OrdinalIgnoreCase))
            {
                displayText = "Not categorised";
            }
            else
            {
                var channelOption = viewModel.ChannelOptions.FirstOrDefault(c => c.Value.Equals(channel, StringComparison.OrdinalIgnoreCase));
                displayText = channelOption?.Text ?? channel; // Use option text or fallback to slug

                // Remove count from display text if present
                if (displayText.Contains(" ("))
                {
                    var lastParenIndex = displayText.LastIndexOf(" (");
                    if (lastParenIndex > 0)
                    {
                        displayText = displayText.Substring(0, lastParenIndex);
                    }
                }
            }

            selectedFilters.Add(new SelectedFilter
            {
                Category = "Channel",
                Value = channel,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "channel", channel)
            });
        }

        // Add type filters
        foreach (var type in viewModel.SelectedTypes)
        {
            string displayText;
            if (type.Equals("__not_categorised__", StringComparison.OrdinalIgnoreCase))
            {
                displayText = "Not categorised";
            }
            else
            {
                var typeOption = viewModel.TypeOptions.FirstOrDefault(t => t.Value.Equals(type, StringComparison.OrdinalIgnoreCase));
                displayText = typeOption?.Text ?? type; // Use option text or fallback to slug

                // Remove count from display text if present
                if (displayText.Contains(" ("))
                {
                    var lastParenIndex = displayText.LastIndexOf(" (");
                    if (lastParenIndex > 0)
                    {
                        displayText = displayText.Substring(0, lastParenIndex);
                    }
                }
            }

            selectedFilters.Add(new SelectedFilter
            {
                Category = "Type",
                Value = type,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "type", type)
            });
        }

        // Add business area filters
        foreach (var group in viewModel.SelectedGroups)
        {
            string displayText;
            if (group.Equals("__not_categorised__", StringComparison.OrdinalIgnoreCase))
            {
                displayText = "Not categorised";
            }
            else
            {
                var groupOption = viewModel.GroupOptions?.FirstOrDefault(g => g.Value.Equals(group, StringComparison.OrdinalIgnoreCase));
                displayText = groupOption?.Text ?? group;
            }

            selectedFilters.Add(new SelectedFilter
            {
                Category = "Business area",
                Value = group,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "group", group)
            });
        }

        // Add sub-business area filters
        foreach (var subgroup in viewModel.SelectedSubgroups)
        {
            selectedFilters.Add(new SelectedFilter
            {
                Category = "Business area",
                Value = subgroup,
                DisplayText = $"{subgroup} (sub-area)",
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "subgroup", subgroup)
            });
        }

        // Add CMDB status filters
        foreach (var status in viewModel.SelectedCmdbStatuses)
        {
            selectedFilters.Add(new SelectedFilter
            {
                Category = "CMDB status",
                Value = status,
                DisplayText = status,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "cmdb-status", status)
            });
        }

        // Add CMDB group filters
        foreach (var group in viewModel.SelectedCmdbGroups)
        {
            var cmdbGroupOption = viewModel.CmdbGroupOptions.FirstOrDefault(cg => cg.Value.Equals(group, StringComparison.OrdinalIgnoreCase));
            var displayText = cmdbGroupOption?.Text ?? group; // Use option text or fallback to slug

            // Remove count from display text if present
            if (displayText.Contains(" ("))
            {
                var lastParenIndex = displayText.LastIndexOf(" (");
                if (lastParenIndex > 0)
                {
                    displayText = displayText.Substring(0, lastParenIndex);
                }
            }

            selectedFilters.Add(new SelectedFilter
            {
                Category = "CMDB group",
                Value = group,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "parent", group)
            });
        }

        viewModel.SelectedFilters = selectedFilters;
    }

    /// <summary>
    /// Parse a free-text keyword query into discrete search terms.
    /// - Only comma-separated segments are treated as separate OR terms (e.g. "Discovery, Alpha").
    /// - Words like "and" / "or" are left inside the terms and not treated as operators.
    /// </summary>
    private static List<string> ParseKeywords(string? rawKeywords)
    {
        if (string.IsNullOrWhiteSpace(rawKeywords))
        {
            return new List<string>();
        }

        // Split on commas to get primary OR terms
        var segments = rawKeywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (segments.Count == 0)
        {
            return new List<string>();
        }

        return segments;
    }

    private string BuildRemoveFilterUrl(ProductsViewModel viewModel, string filterType, string value)
    {
        var queryParams = new List<string>();

        // Handle keywords separately so individual keyword badges can remove a single term
        string? newKeywords = viewModel.Keywords;
        if (!string.IsNullOrEmpty(viewModel.Keywords))
        {
            if (filterType.Equals("keywords", StringComparison.OrdinalIgnoreCase))
            {
                // Remove only the keyword term represented by this badge
                var terms = ParseKeywords(viewModel.Keywords);
                var updatedTerms = terms
                    .Where(t => !t.Equals(value, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                newKeywords = updatedTerms.Any()
                    ? string.Join(", ", updatedTerms)
                    : null;
            }
        }

        if (!string.IsNullOrEmpty(newKeywords))
        {
            queryParams.Add($"keywords={Uri.EscapeDataString(newKeywords)}");
        }

        // Add phase filters (exclude only if this is the filter being removed)
        var phasesToInclude = filterType.Equals("phase", StringComparison.OrdinalIgnoreCase) 
            ? viewModel.SelectedPhases.Where(p => !p.Equals(value, StringComparison.OrdinalIgnoreCase)).ToArray()
            : viewModel.SelectedPhases.ToArray();
        if (phasesToInclude.Length > 0)
            queryParams.AddRange(phasesToInclude.Select(p => $"phase={Uri.EscapeDataString(p)}"));

        // Add group filters (exclude only if this is the filter being removed)
        var groupsToInclude = filterType.Equals("group", StringComparison.OrdinalIgnoreCase)
            ? viewModel.SelectedGroups.Where(g => !g.Equals(value, StringComparison.OrdinalIgnoreCase)).ToArray()
            : viewModel.SelectedGroups.ToArray();
        if (groupsToInclude.Length > 0)
            queryParams.AddRange(groupsToInclude.Select(g => $"group={Uri.EscapeDataString(g)}"));

        // Add subgroup filters (exclude only if this is the filter being removed)
        var subgroupsToInclude = filterType.Equals("subgroup", StringComparison.OrdinalIgnoreCase)
            ? viewModel.SelectedSubgroups.Where(sg => !sg.Equals(value, StringComparison.OrdinalIgnoreCase)).ToArray()
            : viewModel.SelectedSubgroups.ToArray();
        if (subgroupsToInclude.Length > 0)
            queryParams.AddRange(subgroupsToInclude.Select(sg => $"subgroup={Uri.EscapeDataString(sg)}"));

        // Add channel filters (exclude only if this is the filter being removed)
        var channelsToInclude = filterType.Equals("channel", StringComparison.OrdinalIgnoreCase)
            ? viewModel.SelectedChannels.Where(c => !c.Equals(value, StringComparison.OrdinalIgnoreCase)).ToArray()
            : viewModel.SelectedChannels.ToArray();
        if (channelsToInclude.Length > 0)
            queryParams.AddRange(channelsToInclude.Select(c => $"channel={Uri.EscapeDataString(c)}"));

        // Add type filters (exclude only if this is the filter being removed)
        var typesToInclude = filterType.Equals("type", StringComparison.OrdinalIgnoreCase)
            ? viewModel.SelectedTypes.Where(t => !t.Equals(value, StringComparison.OrdinalIgnoreCase)).ToArray()
            : viewModel.SelectedTypes.ToArray();
        if (typesToInclude.Length > 0)
            queryParams.AddRange(typesToInclude.Select(t => $"type={Uri.EscapeDataString(t)}"));

        // Add CMDB status filters (exclude only if this is the filter being removed)
        var cmdbStatusesToInclude = filterType.Equals("cmdbStatus", StringComparison.OrdinalIgnoreCase)
            ? viewModel.SelectedCmdbStatuses.Where(s => !s.Equals(value, StringComparison.OrdinalIgnoreCase)).ToArray()
            : viewModel.SelectedCmdbStatuses.ToArray();
        if (cmdbStatusesToInclude.Length > 0)
            queryParams.AddRange(cmdbStatusesToInclude.Select(s => $"cmdbStatus={Uri.EscapeDataString(s)}"));

        // Add CMDB parent/group filters (exclude only if this is the filter being removed)
        var cmdbGroupsToInclude = filterType.Equals("parent", StringComparison.OrdinalIgnoreCase)
            ? viewModel.SelectedCmdbGroups.Where(g => !g.Equals(value, StringComparison.OrdinalIgnoreCase)).ToArray()
            : viewModel.SelectedCmdbGroups.ToArray();
        if (cmdbGroupsToInclude.Length > 0)
            queryParams.AddRange(cmdbGroupsToInclude.Select(g => $"parent={Uri.EscapeDataString(g)}"));

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        return $"/products{queryString}";
    }

    // GET: Products/Edit/{fipsid}
    [HttpGet]
    public async Task<IActionResult> ProductEdit(string fipsid, [FromQuery] string? returnUrl)
    {
        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            
            // Get the product
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);
            if (product == null)
            {
                _logger.LogWarning("Product not found for FIPS ID: {FipsId}", fipsid);
                return NotFound();
            }

            _logger.LogInformation("Loaded product for FIPS ID: {FipsId}, Product ID: {ProductId}, DocumentId: {DocumentId}, Title: {Title}, CategoryValues Count: {CategoryCount}", 
                fipsid, product.Id, product.DocumentId, product.Title, product.CategoryValues?.Count ?? 0);

            // Get category values for all types
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();

            // Note: Users API call removed since contacts section was removed

            // Note: Product contacts removed since contacts section was removed

            // Find current category values
            var currentPhase = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
            var currentGroup = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true);
            var currentChannels = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Channel", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();
            var currentTypes = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();

            var viewModel = new ProductEditViewModel
            {
                Product = product,
                Title = product.Title,
                ShortDescription = product.ShortDescription,
                LongDescription = product.LongDescription,
                SelectedPhaseId = currentPhase?.Id,
                SelectedGroupId = currentGroup?.Id,
                SelectedChannelIds = currentChannels.Select(c => c.Id).ToList(),
                SelectedTypeIds = currentTypes.Select(c => c.Id).ToList(),
                AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList(),
                AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList(),
                AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList(),
                AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList(),
                PageTitle = $"Edit {product.Title}",
                PageDescription = $"Edit details, categories and contacts for {product.Title}"
            };

            ViewData["ActiveNav"] = "products";
            ViewData["EditProductEnabled"] = _enabledFeatures.EditProduct;
            ViewData["ReturnUrl"] = returnUrl; // Pass returnUrl to the view
            return View("~/Views/Product/edit.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product edit form for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }

    // POST: Products/Edit/{fipsid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductEdit(string fipsid, ProductEditViewModel model)
    {
        _logger.LogInformation("ProductEdit POST method called with FIPS ID: {FipsId}", fipsid);
        _logger.LogInformation("Model data - Title: {Title}, ShortDescription: {ShortDesc}, LongDescription: {LongDesc}, SelectedPhaseId: {PhaseId}, SelectedGroupId: {GroupId}", 
            model.Title, model.ShortDescription, model.LongDescription, model.SelectedPhaseId, model.SelectedGroupId);
        _logger.LogInformation("ModelState.IsValid: {IsValid}, ModelState Error Count: {ErrorCount}", 
            ModelState.IsValid, ModelState.ErrorCount);
        
        try
        {
            if (!ModelState.IsValid)
            {
                // Reload the form data
                var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
                var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
                var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
                var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
                var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();
                // Note: Users API call removed since contacts section was removed

                model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();
                model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
                model.AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList();
                model.AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList();

                ViewData["ActiveNav"] = "products";
                return View("~/Views/Product/edit.cshtml", model);
            }

            // Get the current product to ensure we have the right ID
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);
            if (product == null)
            {
                return NotFound();
            }

            // Prepare category values list
            var categoryValuesList = new List<int>();
            
            if (model.SelectedPhaseId.HasValue && model.SelectedPhaseId.Value > 0)
            {
                categoryValuesList.Add(model.SelectedPhaseId.Value);
            }
            if (model.SelectedGroupId.HasValue && model.SelectedGroupId.Value > 0)
            {
                categoryValuesList.Add(model.SelectedGroupId.Value);
            }
            // Add multiple channels
            categoryValuesList.AddRange(model.SelectedChannelIds.Where(id => id > 0));
            
            // Add multiple types
            categoryValuesList.AddRange(model.SelectedTypeIds.Where(id => id > 0));

            // Prepare the product data for CMS API
            var productData = new
            {
                data = new
                {
                    title = model.Title,
                    short_description = model.ShortDescription,
                    long_description = model.LongDescription,
                    category_values = categoryValuesList,
                    publishedAt = (DateTime?)null // Keep as draft [[memory:8564229]]
                }
            };

            // Update the product via CMS API
            _logger.LogInformation("Attempting to update product with ID: {ProductId}, DocumentId: {DocumentId}, FipsId: {FipsId}", 
                product.Id, product.DocumentId, product.FipsId);
            
            // Use documentId for the update (this is what Strapi expects for PUT requests)
            string documentId = product.DocumentId ?? throw new InvalidOperationException("Product has no documentId");
            _logger.LogInformation("Using documentId: {DocumentId} for update", documentId);
            
            ApiResponse<Product>? updatedProduct = null;
            try
            {
                updatedProduct = await _cmsApiService.PutAsync<ApiResponse<Product>>($"products/{documentId}", productData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product with documentId: {DocumentId}. Error: {ErrorMessage}", 
                    documentId, ex.Message);
                ModelState.AddModelError("", $"Failed to update the product: {ex.Message}");
                
                // Reload form data and return to view
                return await ReloadEditFormWithData(fipsid, model);
            }

            if (updatedProduct?.Data != null)
            {
                // Note: Product contacts updates removed since contacts section was removed

                TempData["SuccessMessage"] = $"Product '{model.Title}' has been updated successfully.";
                return RedirectToAction("ViewProduct", new { fipsid = fipsid });
            }
            else
            {
                ModelState.AddModelError("", "Failed to update the product. Please try again.");
                
                // Reload form data
                var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
                var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
                var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
                var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
                var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();
                // Note: Users API call removed since contacts section was removed

                model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();
                model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
                model.AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList();
                model.AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList();

                ViewData["ActiveNav"] = "products";
                return View("~/Views/Product/edit.cshtml", model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {Title}", model.Title);
            ModelState.AddModelError("", "An error occurred while updating the product. Please try again.");
            
            // Reload form data
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();
            // Note: Users API call removed since contacts section was removed

            model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();
            model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
            model.AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList();
            model.AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList();

            ViewData["ActiveNav"] = "products";
            return View("~/Views/Product/edit.cshtml", model);
        }
    }

    // Note: UpdateProductContacts method removed since contacts section was removed

    private async Task<IActionResult> ReloadEditFormWithData(string fipsid, ProductEditViewModel model)
    {
        try
        {
            // Get the product again to ensure we have current data
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);
            if (product == null)
            {
                return NotFound();
            }

            // Reload category values
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();

            // Update the model with fresh data
            model.Product = product;
            model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();
            model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
            model.AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList();
            model.AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList();

            // Get current category values for the product
            var currentChannels = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Channel", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();
            var currentTypes = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();

            model.SelectedChannelIds = currentChannels.Select(c => c.Id).ToList();
            model.SelectedTypeIds = currentTypes.Select(c => c.Id).ToList();

            ViewData["ActiveNav"] = "products";
            return View("~/Views/Product/edit.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading edit form for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }

    // GET: Products/ProposeChange/{fipsid}
    [HttpGet]
    public async Task<IActionResult> ProposeChange(string fipsid, [FromQuery] string? returnUrl)
    {
        // Check if EditProduct feature is enabled (reusing same feature flag)
        if (!_enabledFeatures.EditProduct)
        {
            return NotFound();
        }

        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            
            // Get the product
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);
            if (product == null)
            {
                _logger.LogWarning("Product not found for FIPS ID: {FipsId}", fipsid);
                return NotFound();
            }

            // Get category values for all types
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();

            // Find current category values
            var currentPhase = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
            var currentGroup = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true);
            var currentChannels = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Channel", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();
            var currentTypes = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();

            var viewModel = new ProposeChangeViewModel
            {
                Product = product,
                ProposedTitle = product.Title,
                ProposedShortDescription = product.ShortDescription,
                ProposedLongDescription = product.LongDescription,
                ProposedProductUrl = product.ProductUrl,
                ProposedPhaseId = currentPhase?.Id,
                ProposedGroupId = currentGroup?.Id,
                ProposedChannelIds = currentChannels.Select(c => c.Id).ToList(),
                ProposedTypeIds = currentTypes.Select(c => c.Id).ToList(),
                AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList(),
                AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList(),
                AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList(),
                AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList(),
                PageTitle = $"Propose changes to {product.Title}",
                PageDescription = $"Suggest changes to details, categories and contacts for {product.Title}"
            };

            // Convert product contacts to proposed format
            if (product.ProductContacts?.Any() == true)
            {
                viewModel.ProposedProductContacts = product.ProductContacts.Select(pc => new ProposedProductContactModel
                {
                    Role = pc.Role,
                    UserId = pc.UsersPermissionsUser?.Id,
                    UserEmail = pc.UsersPermissionsUser?.Email,
                    UserName = pc.UsersPermissionsUser?.DisplayName ?? $"{pc.UsersPermissionsUser?.FirstName} {pc.UsersPermissionsUser?.LastName}".Trim()
                }).ToList();

                // Populate individual role fields from current contacts
                foreach (var contact in product.ProductContacts)
                {
                    var userName = contact.UsersPermissionsUser?.DisplayName ?? $"{contact.UsersPermissionsUser?.FirstName} {contact.UsersPermissionsUser?.LastName}".Trim();
                    
                    switch (contact.Role?.ToLowerInvariant())
                    {
                        case "senior_responsible_owner":
                            viewModel.ProposedServiceOwner = userName;
                            break;
                        case "delivery_manager":
                            viewModel.ProposedDeliveryManager = userName;
                            break;
                        case "information_asset_owner":
                            viewModel.ProposedInformationAssetOwner = userName;
                            break;
                        case "product_manager":
                            viewModel.ProposedProductManager = userName;
                            break;
                    }
                }
            }

            ViewData["ActiveNav"] = "products";
            ViewData["EditProductEnabled"] = _enabledFeatures.EditProduct;
            ViewData["ReturnUrl"] = returnUrl; // Pass returnUrl to the view
            return View("~/Views/Product/propose-change.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading propose change form for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }

    // POST: Products/ProposeChange/{fipsid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProposeChange(string fipsid, ProposeChangeViewModel model)
    {
        _logger.LogInformation("ProposeChange POST method called with FIPS ID: {FipsId}", fipsid);
        
        // Log all posted values for debugging
        _logger.LogInformation("Posted form values:");
        _logger.LogInformation("  ProposedTitle: '{Value}'", model.ProposedTitle ?? "(null)");
        _logger.LogInformation("  ProposedShortDescription: '{Value}'", model.ProposedShortDescription ?? "(null)");
        _logger.LogInformation("  ProposedLongDescription: '{Value}'", model.ProposedLongDescription?.Substring(0, Math.Min(100, model.ProposedLongDescription?.Length ?? 0)) ?? "(null)");
        _logger.LogInformation("  ProposedProductUrl: '{Value}'", model.ProposedProductUrl ?? "(null)");
        _logger.LogInformation("  ProposedPhaseId: {Value}", model.ProposedPhaseId?.ToString() ?? "(null)");
        _logger.LogInformation("  ProposedGroupId: {Value}", model.ProposedGroupId?.ToString() ?? "(null)");
        _logger.LogInformation("  ProposedChannelIds: [{Values}]", string.Join(", ", model.ProposedChannelIds ?? new List<int>()));
        _logger.LogInformation("  ProposedTypeIds: [{Values}]", string.Join(", ", model.ProposedTypeIds ?? new List<int>()));
        _logger.LogInformation("  Reason: '{Value}'", model.Reason ?? "(null)");
        
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                // Reload the form data
                return await ReloadProposeChangeForm(fipsid, model);
            }

            // Get the current product to ensure we have the right data
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);
            if (product == null)
            {
                return NotFound();
            }

            // Log all available claims for debugging
            _logger.LogInformation("Available claims for user:");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation("  Claim Type: {Type}, Value: {Value}", claim.Type, claim.Value);
            }
            _logger.LogInformation("User.Identity.Name: {Name}, User.Identity.IsAuthenticated: {IsAuth}", 
                User.Identity?.Name ?? "null", User.Identity?.IsAuthenticated ?? false);
            
            // Get user email from claims - check multiple claim types
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value 
                ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == "upn")?.Value
                ?? User.Identity?.Name 
                ?? "test.user@education.gov.uk"; // Changed default for testing
            
            var userName = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value 
                ?? User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value
                ?? User.Identity?.Name 
                ?? "Test User"; // Changed default for testing
            
            _logger.LogInformation("Processing proposed change from user: {UserName} ({UserEmail})", userName, userEmail);
            
            // Warning if using defaults
            if (userEmail == "test.user@education.gov.uk")
            {
                _logger.LogWarning("Using default test user credentials - authentication may be disabled");
            }

            // Build proposed category values changes
            var proposedCategoryValues = new List<ProposedCategoryValueChange>();
            var currentCategoryValues = new List<ProposedCategoryValueChange>();

            // Get current category values
            var currentPhase = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
            var currentGroup = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true);
            var currentChannels = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Channel", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();
            var currentTypes = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();

            // Store current values
            if (currentPhase != null)
            {
                currentCategoryValues.Add(new ProposedCategoryValueChange
                {
                    CategoryType = "Phase",
                    CategoryValueIds = new List<int> { currentPhase.Id },
                    CategoryValueNames = new List<string> { currentPhase.Name }
                });
            }
            if (currentGroup != null)
            {
                currentCategoryValues.Add(new ProposedCategoryValueChange
                {
                    CategoryType = "Business area",
                    CategoryValueIds = new List<int> { currentGroup.Id },
                    CategoryValueNames = new List<string> { currentGroup.Name }
                });
            }
            if (currentChannels.Any())
            {
                currentCategoryValues.Add(new ProposedCategoryValueChange
                {
                    CategoryType = "Channel",
                    CategoryValueIds = currentChannels.Select(c => c.Id).ToList(),
                    CategoryValueNames = currentChannels.Select(c => c.Name).ToList()
                });
            }
            if (currentTypes.Any())
            {
                currentCategoryValues.Add(new ProposedCategoryValueChange
                {
                    CategoryType = "Type",
                    CategoryValueIds = currentTypes.Select(t => t.Id).ToList(),
                    CategoryValueNames = currentTypes.Select(t => t.Name).ToList()
                });
            }

            // Get category values duration for lookups
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            
            // Store proposed values
            if (model.ProposedPhaseId.HasValue)
            {
                var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
                var phase = phaseValues.FirstOrDefault(p => p.Id == model.ProposedPhaseId.Value);
                if (phase != null)
                {
                    proposedCategoryValues.Add(new ProposedCategoryValueChange
                    {
                        CategoryType = "Phase",
                        CategoryValueIds = new List<int> { phase.Id },
                        CategoryValueNames = new List<string> { phase.Name }
                    });
                }
            }
            if (model.ProposedGroupId.HasValue)
            {
                var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
                var group = groupValues.FirstOrDefault(g => g.Id == model.ProposedGroupId.Value);
                if (group != null)
                {
                    proposedCategoryValues.Add(new ProposedCategoryValueChange
                    {
                        CategoryType = "Business area",
                        CategoryValueIds = new List<int> { group.Id },
                        CategoryValueNames = new List<string> { group.Name }
                    });
                }
            }
            if (model.ProposedChannelIds?.Any() == true)
            {
                var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
                var channels = channelValues.Where(c => model.ProposedChannelIds.Contains(c.Id)).ToList();
                if (channels.Any())
                {
                    proposedCategoryValues.Add(new ProposedCategoryValueChange
                    {
                        CategoryType = "Channel",
                        CategoryValueIds = channels.Select(c => c.Id).ToList(),
                        CategoryValueNames = channels.Select(c => c.Name).ToList()
                    });
                }
            }
            if (model.ProposedTypeIds?.Any() == true)
            {
                var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();
                var types = typeValues.Where(t => model.ProposedTypeIds.Contains(t.Id)).ToList();
                if (types.Any())
                {
                    proposedCategoryValues.Add(new ProposedCategoryValueChange
                    {
                        CategoryType = "Type",
                        CategoryValueIds = types.Select(t => t.Id).ToList(),
                        CategoryValueNames = types.Select(t => t.Name).ToList()
                    });
                }
            }

            // Build proposed contacts changes for display
            var proposedContactNames = model.ProposedProductContacts?
                .Select(pc => $"{pc.Role}: {pc.UserName} ({pc.UserEmail})")
                .ToList();
            
            var currentContactNames = product.ProductContacts?
                .Select(pc => $"{pc.Role}: {pc.UsersPermissionsUser?.DisplayName ?? $"{pc.UsersPermissionsUser?.FirstName} {pc.UsersPermissionsUser?.LastName}".Trim()} ({pc.UsersPermissionsUser?.Email})")
                .ToList();

            // Send email notification via GOV.UK Notify
            try
            {
                // Build the change table for the email
                var currentCategoryNames = currentCategoryValues
                    .SelectMany(ccv => ccv.CategoryValueNames.Select(name => $"{ccv.CategoryType}: {name}"))
                    .ToList();
                var proposedCategoryNames = proposedCategoryValues
                    .SelectMany(pcv => pcv.CategoryValueNames.Select(name => $"{pcv.CategoryType}: {name}"))
                    .ToList();

                // Log what we're sending to the change table builder
                _logger.LogInformation("Building change table with:");
                _logger.LogInformation("  Title: '{Current}' -> '{Proposed}'", product.Title, model.ProposedTitle);
                _logger.LogInformation("  Short Desc: '{Current}' -> '{Proposed}'", product.ShortDescription, model.ProposedShortDescription);
                _logger.LogInformation("  Long Desc: '{Current}' -> '{Proposed}'", 
                    product.LongDescription?.Substring(0, Math.Min(50, product.LongDescription?.Length ?? 0)) ?? "(null)", 
                    model.ProposedLongDescription?.Substring(0, Math.Min(50, model.ProposedLongDescription?.Length ?? 0)) ?? "(null)");
                _logger.LogInformation("  Product URL: '{Current}' -> '{Proposed}'", product.ProductUrl, model.ProposedProductUrl);
                _logger.LogInformation("  Current Categories ({Count}): {Categories}", currentCategoryNames.Count, string.Join(", ", currentCategoryNames));
                _logger.LogInformation("  Proposed Categories ({Count}): {Categories}", proposedCategoryNames.Count, string.Join(", ", proposedCategoryNames));
                _logger.LogInformation("  Current Contacts ({Count}): {Contacts}", currentContactNames?.Count ?? 0, string.Join(", ", currentContactNames ?? new List<string>()));
                _logger.LogInformation("  Proposed Contacts ({Count}): {Contacts}", proposedContactNames?.Count ?? 0, string.Join(", ", proposedContactNames ?? new List<string>()));

                var changeTableMarkdown = NotifyService.BuildChangeTableHtml(
                    product.Title, 
                    model.ProposedTitle,
                    product.ShortDescription, 
                    model.ProposedShortDescription,
                    product.LongDescription, 
                    model.ProposedLongDescription,
                    product.ProductUrl, 
                    model.ProposedProductUrl,
                    currentCategoryNames, 
                    proposedCategoryNames,
                    currentContactNames, 
                    proposedContactNames,
                    null, // currentUserDescription - not available in Product model yet
                    model.ProposedUserDescription,
                    null, // currentServiceOwner - not available in Product model yet
                    model.ProposedServiceOwner,
                    null, // currentInformationAssetOwner - not available in Product model yet
                    model.ProposedInformationAssetOwner,
                    null, // currentDeliveryManager - not available in Product model yet
                    model.ProposedDeliveryManager,
                    null, // currentProductManager - not available in Product model yet
                    model.ProposedProductManager);
                
                _logger.LogInformation("Generated change table markdown: {Markdown}", changeTableMarkdown);

                // Build the entry link
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var entryLink = $"{baseUrl}/product/{fipsid}";

                // Format requestor information
                var requestorInfo = !string.IsNullOrEmpty(userEmail) 
                    ? $"{userName} ({userEmail})" 
                    : userName;

                await _notifyService.SendProposedChangeEmailAsync(
                    fipsid ?? product.DocumentId ?? "Unknown",
                    product.Title,
                    entryLink,
                    changeTableMarkdown,
                    requestorInfo,
                    model.Reason,
                    product.CmdbSysId);

                _logger.LogInformation("Email notification sent successfully for proposed change to product {FipsId} from {Requestor}", fipsid, requestorInfo);

                // Stay on the same page and show success message
                ViewData["SuccessMessage"] = "Your proposed changes have been submitted successfully and will be reviewed by an administrator.";
                
                // Reload the form with fresh data
                return await ReloadProposeChangeForm(fipsid, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification for proposed change to product {FipsId}. Error: {ErrorMessage}", fipsid, ex.Message);
                ModelState.AddModelError("", $"Failed to submit the proposed changes: {ex.Message}");
                
                // Stay on the same page and show error
                return await ReloadProposeChangeForm(fipsid, model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing proposed change: {Title}", model.Product?.Title);
            ModelState.AddModelError("", "An error occurred while processing your proposed changes. Please try again.");
            return await ReloadProposeChangeForm(fipsid, model);
        }
    }

    private async Task<IActionResult> ReloadProposeChangeForm(string fipsid, ProposeChangeViewModel model)
    {
        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);
            if (product == null)
            {
                return NotFound();
            }

            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();

            model.Product = product;
            model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();
            model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
            model.AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList();
            model.AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList();

            // Populate individual role fields from current contacts if not already set
            if (product.ProductContacts?.Any() == true)
            {
                foreach (var contact in product.ProductContacts)
                {
                    var userName = contact.UsersPermissionsUser?.DisplayName ?? $"{contact.UsersPermissionsUser?.FirstName} {contact.UsersPermissionsUser?.LastName}".Trim();
                    
                    switch (contact.Role?.ToLowerInvariant())
                    {
                        case "senior_responsible_owner":
                            if (string.IsNullOrEmpty(model.ProposedServiceOwner))
                                model.ProposedServiceOwner = userName;
                            break;
                        case "delivery_manager":
                            if (string.IsNullOrEmpty(model.ProposedDeliveryManager))
                                model.ProposedDeliveryManager = userName;
                            break;
                        case "information_asset_owner":
                            if (string.IsNullOrEmpty(model.ProposedInformationAssetOwner))
                                model.ProposedInformationAssetOwner = userName;
                            break;
                        case "product_manager":
                            if (string.IsNullOrEmpty(model.ProposedProductManager))
                                model.ProposedProductManager = userName;
                            break;
                    }
                }
            }

            ViewData["ActiveNav"] = "products";
            ViewData["EditProductEnabled"] = _enabledFeatures.EditProduct;
            return View("~/Views/Product/propose-change.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading propose change form for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }

    // GET: Products/RequestNewEntry
    [HttpGet]
    public async Task<IActionResult> RequestNewEntry()
    {
        // Check if EditProduct feature is enabled (reusing same feature flag for now)
        if (!_enabledFeatures.EditProduct)
        {
            return NotFound();
        }

        try
        {
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();

            var viewModel = new RequestNewEntryViewModel
            {
                AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList(),
                AvailableBusinessAreas = groupValues.Where(cv => cv.Enabled).ToList(),
                AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList(),
                AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList(),
                PageTitle = "Request a new product entry",
                PageDescription = "Submit a request to add a new product to FIPS"
            };

            ViewData["ActiveNav"] = "products";
            ViewData["EditProductEnabled"] = _enabledFeatures.EditProduct;
            return View("~/Views/Product/request-new-entry.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading request new entry form");
            return NotFound();
        }
    }

    // POST: Products/RequestNewEntry
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestNewEntry(RequestNewEntryViewModel model)
    {
        _logger.LogInformation("RequestNewEntry POST method called");
        
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                // Reload the form data
                return await ReloadRequestNewEntryForm(model);
            }

            // Get user information
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value 
                ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == "upn")?.Value
                ?? User.Identity?.Name 
                ?? "test.user@education.gov.uk";
            
            var userName = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value 
                ?? User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                ?? User.Identity?.Name
                ?? "Test User";

            // Format requestor information
            var requestorInfo = !string.IsNullOrEmpty(userEmail) 
                ? $"{userName} ({userEmail})" 
                : userName;

            // Get category values for selected options
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();

            // Build friendly names for selected values
            var phaseName = model.PhaseId.HasValue 
                ? phaseValues.FirstOrDefault(pv => pv.Id == model.PhaseId.Value)?.Name 
                : null;
            
            var businessAreaName = model.BusinessAreaId.HasValue 
                ? groupValues.FirstOrDefault(gv => gv.Id == model.BusinessAreaId.Value)?.Name 
                : null;
            
            var channelNames = model.ChannelIds.Any() 
                ? string.Join(", ", channelValues.Where(cv => model.ChannelIds.Contains(cv.Id)).Select(cv => cv.Name)) 
                : null;
            
            var typeNames = model.TypeIds.Any() 
                ? string.Join(", ", typeValues.Where(tv => model.TypeIds.Contains(tv.Id)).Select(tv => tv.Name)) 
                : null;

            try
            {
                await _notifyService.SendNewEntryRequestEmailAsync(
                    requestorInfo,
                    model.Title,
                    model.Description,
                    phaseName,
                    businessAreaName,
                    channelNames,
                    typeNames,
                    model.ServiceUrl,
                    model.Users,
                    model.DeliveryManager,
                    model.ProductManager,
                    model.SeniorResponsibleOfficer,
                    model.Notes);

                _logger.LogInformation("New entry request email sent successfully for title: {Title} from {Requestor}", model.Title, requestorInfo);

                // Show success message and stay on the page
                ViewData["SuccessMessage"] = "Your new product entry request has been submitted successfully. The FIPS team will review your request and may contact you if additional information is needed.";
                
                // Clear the form by creating a new empty model but keep the available options
                return await ReloadRequestNewEntryForm(new RequestNewEntryViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new entry request email for title: {Title}. Error: {ErrorMessage}", model.Title, ex.Message);
                ModelState.AddModelError("", $"Failed to submit the new entry request: {ex.Message}");
                
                return await ReloadRequestNewEntryForm(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing new entry request: {Title}", model.Title);
            ModelState.AddModelError("", "An error occurred while processing your new entry request. Please try again.");
            return await ReloadRequestNewEntryForm(model);
        }
    }

    private async Task<IActionResult> ReloadRequestNewEntryForm(RequestNewEntryViewModel model)
    {
        try
        {
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Business area", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();

            model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();
            model.AvailableBusinessAreas = groupValues.Where(cv => cv.Enabled).ToList();
            model.AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList();
            model.AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList();

            ViewData["ActiveNav"] = "products";
            ViewData["EditProductEnabled"] = _enabledFeatures.EditProduct;
            return View("~/Views/Product/request-new-entry.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading request new entry form");
            return NotFound();
        }
    }

    /// <summary>
    /// Gets the client IP address from the request, handling proxy headers.
    /// </summary>
    private string GetClientIpAddress()
    {
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

}
