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

    public ProductsController(ILogger<ProductsController> logger, CmsApiService cmsApiService, IOptimizedCmsApiService optimizedCmsApiService, IOptions<EnabledFeatures> enabledFeatures, IConfiguration configuration)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _optimizedCmsApiService = optimizedCmsApiService;
        _enabledFeatures = enabledFeatures.Value;
        _configuration = configuration;
    }

    // GET: Products
    public async Task<IActionResult> Index(string? keywords, string[]? phase, string[]? group, string[]? subgroup,
        string[]? channel, string[]? type, string[]? cmdbStatus, string[]? parent, string[]? userGroup, int page = 1)
    {
        try
        {
            var viewModel = new ProductsViewModel();

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
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", categoryValuesDuration) ?? new List<CategoryValue>();
            // Try different variations of user group category type names
            var userGroupVariations = new[] { "User group", "User Group", "User groups", "User Groups", "User Type", "User Types", "Audience", "Target Audience" };
            List<CategoryValue> userGroupValues = new List<CategoryValue>();
            
            foreach (var variation in userGroupVariations)
            {
                var values = await _optimizedCmsApiService.GetCategoryValuesForFilter(variation, categoryValuesDuration) ?? new List<CategoryValue>();
                if (values.Any())
                {
                    _logger.LogInformation("Found {Count} values for category type '{Type}'", values.Count, variation);
                    
                    // Search specifically for "Social worker" in these values
                    var socialWorkerExact = values.FirstOrDefault(v => v.Name.Equals("Social worker", StringComparison.OrdinalIgnoreCase));
                    if (socialWorkerExact != null)
                    {
                        _logger.LogInformation("✅ Found exact 'Social worker' entry: '{Name}' (Slug: {Slug})", socialWorkerExact.Name, socialWorkerExact.Slug);
                    }
                    else
                    {
                        _logger.LogWarning("❌ 'Social worker' not found in {Count} values for category type '{Type}'", values.Count, variation);
                        
                        // Log all values that contain "social" to help debug
                        var socialRelated = values.Where(v => v.Name.ToLower().Contains("social")).ToList();
                        if (socialRelated.Any())
                        {
                            _logger.LogInformation("Social-related entries found:");
                            foreach (var social in socialRelated)
                            {
                                _logger.LogInformation("  - '{Name}' (Slug: {Slug})", social.Name, social.Slug);
                            }
                        }
                    }
                    
                    // Log first few values to see what we're getting
                    var firstFew = values.Take(5);
                    foreach (var value in firstFew)
                    {
                        _logger.LogInformation("Sample user group value: '{Name}' (Slug: {Slug})", value.Name, value.Slug);
                    }
                    userGroupValues = values;
                    break;
                }
                else
                {
                    _logger.LogInformation("No values found for category type '{Type}'", variation);
                }
            }
            
            // Always check for "Social worker" specifically, even if we found user group values
            _logger.LogInformation("Searching for 'Social worker' in all category values...");
            var allCategoryValues = await _optimizedCmsApiService.GetAllCategoryValues(categoryValuesDuration) ?? new List<CategoryValue>();
            var socialWorkerValues = allCategoryValues.Where(cv => 
                cv.Name.Equals("Social worker", StringComparison.OrdinalIgnoreCase) ||
                cv.Name.Contains("Social worker", StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            if (socialWorkerValues.Any())
            {
                _logger.LogInformation("Found {Count} 'Social worker' related values:", socialWorkerValues.Count);
                foreach (var value in socialWorkerValues)
                {
                    _logger.LogInformation("Social worker value: '{Name}' (Slug: {Slug}, CategoryType: {CategoryType})", 
                        value.Name, value.Slug, value.CategoryType?.Name ?? "Unknown");
                }
            }
            else
            {
                _logger.LogWarning("No 'Social worker' values found in any category type");
                
                // Log total count of all category values
                _logger.LogInformation("Total category values in CMS: {Count}", allCategoryValues.Count);
                
                // Log all category types and their counts
                var categoryTypeCounts = allCategoryValues.GroupBy(cv => cv.CategoryType?.Name ?? "Unknown")
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Type);
                
                foreach (var typeCount in categoryTypeCounts)
                {
                    _logger.LogInformation("Category type '{Type}': {Count} values", typeCount.Type, typeCount.Count);
                }
            }
            
            if (!userGroupValues.Any())
            {
                _logger.LogWarning("No user group values found for any of the tested category type names");
            }

            // Debug logging for category values
            _logger.LogInformation("Category values loaded: Phase={PhaseCount}, Channel={ChannelCount}, Group={GroupCount}, UserGroup={UserGroupCount}", 
                phaseValues.Count, channelValues.Count, groupValues.Count, userGroupValues.Count);

            // Build CMS filter query string
            var filterQuery = BuildCmsFilterQuery(phase, group, subgroup, channel, type, cmdbStatus, parent, userGroup);

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
            
            // Convert category filters to server-side format
            if (phase?.Length > 0)
            {
                serverFilters["category_values.slug"] = phase;
            }
            if (channel?.Length > 0)
            {
                serverFilters["category_values.slug"] = (serverFilters.ContainsKey("category_values.slug") ? serverFilters["category_values.slug"].ToList() : new List<string>()).Concat(channel).ToArray();
            }
            if (type?.Length > 0)
            {
                serverFilters["category_values.slug"] = (serverFilters.ContainsKey("category_values.slug") ? serverFilters["category_values.slug"].ToList() : new List<string>()).Concat(type).ToArray();
            }
            if (group?.Length > 0)
            {
                serverFilters["category_values.slug"] = (serverFilters.ContainsKey("category_values.slug") ? serverFilters["category_values.slug"].ToList() : new List<string>()).Concat(group).ToArray();
            }
            if (subgroup?.Length > 0)
            {
                serverFilters["category_values.slug"] = (serverFilters.ContainsKey("category_values.slug") ? serverFilters["category_values.slug"].ToList() : new List<string>()).Concat(subgroup).ToArray();
            }
            if (parent?.Length > 0)
            {
                serverFilters["category_values.slug"] = (serverFilters.ContainsKey("category_values.slug") ? serverFilters["category_values.slug"].ToList() : new List<string>()).Concat(parent).ToArray();
            }
            if (userGroup?.Length > 0)
            {
                serverFilters["category_values.slug"] = (serverFilters.ContainsKey("category_values.slug") ? serverFilters["category_values.slug"].ToList() : new List<string>()).Concat(userGroup).ToArray();
            }

            // Use optimized service for search with caching and server-side filtering
            var searchDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Search", 2));
            var productsDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Products", 15));
            
            var cacheDuration = !string.IsNullOrEmpty(keywords) ? searchDuration : productsDuration;
            
            // Get products with server-side filtering and pagination
            var result = await _optimizedCmsApiService.GetProductsForListingAsync2(cmsPage, pageSize, keywords, serverFilters, cacheDuration);
            filteredProducts = result.Products;
            filteredTotalCount = result.TotalCount;
            totalCount = result.TotalCount;

            // Ensure view model properties are initialized
            viewModel.PhaseOptions = new List<FilterOption>();
            viewModel.GroupOptions = new List<FilterOption>();
            viewModel.ChannelOptions = new List<FilterOption>();
            viewModel.TypeOptions = new List<FilterOption>();
            viewModel.CmdbStatusOptions = new List<FilterOption>();
            viewModel.CmdbGroupOptions = new List<FilterOption>();
            viewModel.UserGroupOptions = new List<FilterOption>();
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
            viewModel.Keywords = keywords;
            viewModel.SelectedPhases = phase?.ToList() ?? new List<string>();
            viewModel.SelectedGroups = group?.ToList() ?? new List<string>();
            viewModel.SelectedSubgroups = subgroup?.ToList() ?? new List<string>();
            viewModel.SelectedChannels = channel?.ToList() ?? new List<string>();
            viewModel.SelectedTypes = type?.ToList() ?? new List<string>();
            viewModel.SelectedCmdbStatuses = cmdbStatus?.ToList() ?? new List<string>();
            viewModel.SelectedCmdbGroups = parent?.ToList() ?? new List<string>();
            viewModel.SelectedUserGroups = userGroup?.ToList() ?? new List<string>();

            // Build filter options - get all products for counting, not just the current page
            var allProductsForCounting = await _optimizedCmsApiService.GetProductsForFilterCountsAsync(productsDuration, "Active");
            await BuildFilterOptions(viewModel, allProductsForCounting, phaseValues, channelValues, groupValues, userGroupValues);

            // Build selected filters for display
            BuildSelectedFilters(viewModel);

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

    // GET: Products/View/{fipsid}
    public async Task<IActionResult> ViewProduct(string fipsid)
    {

        Console.WriteLine("fipsid: " + fipsid);

        // Check if documentId is null or empty
        if (string.IsNullOrEmpty(fipsid))
        {
            return NotFound();
        }

        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            // Find product by fips_id - optimized to return only needed fields
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductViewModel
            {
                Product = product,
                PageTitle = product.Title,
                PageDescription = $"View detailed information about {product.Title}"
            };
            ViewData["ActiveNav"] = "products";
            ViewData["AssuranceEnabled"] = _enabledFeatures.Assurance;
            return View("~/Views/Product/index.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product details for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }

    public async Task<IActionResult> ProductCategories(string fipsid)
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
                var groupedCategories = product.CategoryValues
                    .GroupBy(cv => cv.CategoryType?.Name ?? "Unknown")
                    .OrderBy(g => g.Key);

                foreach (var group in groupedCategories)
                {
                    var categoryType = group.First().CategoryType;
                    if (categoryType != null)
                    {
                        var info = new ProductCategoryInfo
                        {
                            CategoryTypeName = categoryType.Name,
                            CategoryTypeSlug = categoryType.Slug,
                            IsMultiLevel = categoryType.MultiLevel,
                            CategoryValueNames = group.Select(cv => cv.Name).ToList(),
                            CategoryValueSlugs = group.Select(cv => cv.Slug).ToList()
                        };
                        categoryInfo.Add(info);
                    }
                }
            }

            var viewModel = new ProductCategoriesViewModel
            {
                Product = product,
                CategoryInfo = categoryInfo,
                PageTitle = $"{product.Title} - Categories",
                PageDescription = $"View all categories and values assigned to {product.Title}"
            };

            ViewData["ActiveNav"] = "products";
            ViewData["AssuranceEnabled"] = _enabledFeatures.Assurance;
            return View("~/Views/Product/categories.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product categories for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }

    public async Task<IActionResult> ProductAssurance(string fipsid)
    {
        // Check if Assurance feature is enabled
        if (!_enabledFeatures.Assurance)
        {
            return NotFound();
        }

        try
        {
            var productDetailDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:ProductDetail", 10));
            // Find product by fips_id with product assurances populated - optimized for assurance view
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(fipsid, productDetailDuration);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductAssuranceViewModel
            {
                Product = product,
                ProductAssurances = product.ProductAssurances ?? new List<ProductAssurance>(),
                PageTitle = $"{product.Title} - Assurance",
                PageDescription = $"View assurance information for {product.Title}"
            };

            ViewData["ActiveNav"] = "products";
            ViewData["AssuranceEnabled"] = _enabledFeatures.Assurance;
            return View("~/Views/Product/assurance.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product assurance for FIPS ID: {FipsId}", fipsid);
            return NotFound();
        }
    }
    private List<Product> ApplyFilters(List<Product> products, string? keywords, string[]? phase, string[]? group,
        string[]? subgroup, string[]? channel, string[]? type, string[]? cmdbStatus, string[]? parent, string[]? userGroup)
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

        // Apply group/subgroup filters (based on category values with category type "Group")
        if (group?.Length > 0 || subgroup?.Length > 0)
        {
            filtered = filtered.Where(p =>
                p.CategoryValues?.Any(cv =>
                    cv.CategoryType?.Name.Equals("Group", StringComparison.OrdinalIgnoreCase) == true &&
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

        // Apply user group filter (based on category values)
        if (userGroup?.Length > 0)
        {
            filtered = filtered.Where(p =>
                p.CategoryValues?.Any(cv =>
                    userGroup.Contains(cv.Slug, StringComparer.OrdinalIgnoreCase)
                ) == true);
        }

        return filtered.ToList();
    }

    private string BuildCmsFilterQuery(string[]? phase, string[]? group, string[]? subgroup, string[]? channel,
        string[]? type, string[]? cmdbStatus, string[]? parent, string[]? userGroup)
    {
        var filters = new List<string>();

        // Apply phase filter (based on category values with category type "Phase")
        if (phase?.Length > 0)
        {
            foreach (var p in phase)
            {
                filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(p)}");
            }
        }

        // Apply channel filter (based on category values with category type "Channel")
        if (channel?.Length > 0)
        {
            foreach (var c in channel)
            {
                filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(c)}");
            }
        }

        // Apply type filter (based on category values with category type "Type")
        if (type?.Length > 0)
        {
            foreach (var t in type)
            {
                filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(t)}");
            }
        }

        // Apply CMDB status filter (based on state)
        if (cmdbStatus?.Length > 0)
        {
            foreach (var s in cmdbStatus)
            {
                filters.Add($"filters[state][$eq]={Uri.EscapeDataString(s)}");
            }
        }

        // Apply group/subgroup filters (based on category values with category type "Group")
        if (group?.Length > 0 || subgroup?.Length > 0)
        {
            var allGroupSlugs = new List<string>();
            if (group?.Length > 0) allGroupSlugs.AddRange(group);
            if (subgroup?.Length > 0) allGroupSlugs.AddRange(subgroup);

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
                filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(p)}");
            }
        }

        // Apply user group filter (based on category values)
        if (userGroup?.Length > 0)
        {
            foreach (var u in userGroup)
            {
                filters.Add($"filters[category_values][slug][$in]={Uri.EscapeDataString(u)}");
            }
        }

        return string.Join("&", filters);
    }

    private async Task BuildFilterOptions(ProductsViewModel viewModel, List<Product> allProductsForCounting,
        List<CategoryValue> phaseValues, List<CategoryValue> channelValues,
        List<CategoryValue> groupValues, List<CategoryValue> userGroupValues)
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
                    Text = actualCount > 0 ? $"{pv.Name} ({actualCount})" : pv.Name,
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
                    Text = actualCount > 0 ? $"{cv.Name} ({actualCount})" : cv.Name,
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
                    Text = actualCount > 0 ? $"{tv.Name} ({actualCount})" : tv.Name,
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
            Text = $"{g.Key} ({g.Count()})",
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

        // Build user group options from specific user group values (hierarchical)
        _logger.LogInformation("Processing {Count} user group values", userGroupValues.Count);

        if (userGroupValues.Any())
        {
            viewModel.UserGroupOptions = new List<FilterOption>();
            foreach (var gv in userGroupValues)
            {
                var displayText = gv.Name;
                var parentInfo = "";

                // Add parent information for child values
                if (gv.Parent != null)
                {
                    parentInfo = $" ({gv.Parent.Name})";
                    displayText = $"{gv.Name} {parentInfo}";
                }

                // Count products with this user group value locally (much faster)
                var actualCount = allProductsForCounting.Count(p => p.CategoryValues?.Any(categoryValue =>
                    categoryValue.Slug.Equals(gv.Slug, StringComparison.OrdinalIgnoreCase)) == true);

                _logger.LogDebug("User group '{Name}' (slug: {Slug}) has {Count} products", gv.Name, gv.Slug, actualCount);

                // Count children for this user group
                var childCount = userGroupValues.Count(child => child.Parent?.Slug == gv.Slug);
                
                viewModel.UserGroupOptions.Add(new FilterOption
                {
                    Value = gv.Slug,
                    Text = actualCount > 0 ? $"{displayText} ({actualCount})" : displayText,
                    Count = actualCount,
                    IsSelected = viewModel.SelectedUserGroups.Contains(gv.Slug, StringComparer.OrdinalIgnoreCase),
                    ParentName = gv.Parent?.Name,
                    HasChildren = childCount > 0,
                    ChildCount = childCount
                });
            }
            
            _logger.LogInformation("Added {Count} user group options to view model", viewModel.UserGroupOptions.Count);
        }
        else
        {
            _logger.LogWarning("No user group values found - UserGroupOptions will be empty");
        }
    }

    private int CountProductsWithCategoryValue(List<Product> products, string categoryValueSlug)
    {
        return products.Count(p => p.CategoryValues?.Any(cv =>
            cv.Slug.Equals(categoryValueSlug, StringComparison.OrdinalIgnoreCase)) == true);
    }

    private void BuildSelectedFilters(ProductsViewModel viewModel)
    {
        var selectedFilters = new List<SelectedFilter>();

        // Add phase filters
        foreach (var phase in viewModel.SelectedPhases)
        {
            var phaseOption = viewModel.PhaseOptions.FirstOrDefault(p => p.Value.Equals(phase, StringComparison.OrdinalIgnoreCase));
            var displayText = phaseOption?.Text ?? phase; // Use option text or fallback to slug

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
                Category = "Phase",
                Value = phase,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "phase", phase)
            });
        }

        // Add channel filters
        foreach (var channel in viewModel.SelectedChannels)
        {
            var channelOption = viewModel.ChannelOptions.FirstOrDefault(c => c.Value.Equals(channel, StringComparison.OrdinalIgnoreCase));
            var displayText = channelOption?.Text ?? channel; // Use option text or fallback to slug

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
                Category = "Channel",
                Value = channel,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "channel", channel)
            });
        }

        // Add type filters
        foreach (var type in viewModel.SelectedTypes)
        {
            var typeOption = viewModel.TypeOptions.FirstOrDefault(t => t.Value.Equals(type, StringComparison.OrdinalIgnoreCase));
            var displayText = typeOption?.Text ?? type; // Use option text or fallback to slug

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
                Category = "Type",
                Value = type,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "type", type)
            });
        }

        // Add group filters
        foreach (var group in viewModel.SelectedGroups)
        {
            selectedFilters.Add(new SelectedFilter
            {
                Category = "Group",
                Value = group,
                DisplayText = group,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "group", group)
            });
        }

        // Add subgroup filters
        foreach (var subgroup in viewModel.SelectedSubgroups)
        {
            selectedFilters.Add(new SelectedFilter
            {
                Category = "Group",
                Value = subgroup,
                DisplayText = $"{subgroup} (subgroup)",
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

        // Add user group filters
        foreach (var userGroup in viewModel.SelectedUserGroups)
        {
            var userGroupOption = viewModel.UserGroupOptions.FirstOrDefault(ug => ug.Value.Equals(userGroup, StringComparison.OrdinalIgnoreCase));
            var displayText = userGroupOption?.Text ?? userGroup; // Use option text or fallback to slug

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
                Category = "User groups",
                Value = userGroup,
                DisplayText = displayText,
                RemoveUrl = BuildRemoveFilterUrl(viewModel, "userGroup", userGroup)
            });
        }

        viewModel.SelectedFilters = selectedFilters;
    }

    private string BuildRemoveFilterUrl(ProductsViewModel viewModel, string filterType, string value)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(viewModel.Keywords))
            queryParams.Add($"keywords={Uri.EscapeDataString(viewModel.Keywords)}");

        // Add other filters except the one being removed
        var otherPhases = viewModel.SelectedPhases.Where(p => p != value).ToArray();
        if (otherPhases.Length > 0)
            queryParams.AddRange(otherPhases.Select(p => $"phase={Uri.EscapeDataString(p)}"));

        var otherGroups = viewModel.SelectedGroups.Where(g => g != value).ToArray();
        if (otherGroups.Length > 0)
            queryParams.AddRange(otherGroups.Select(g => $"group={Uri.EscapeDataString(g)}"));

        var otherSubgroups = viewModel.SelectedSubgroups.Where(sg => sg != value).ToArray();
        if (otherSubgroups.Length > 0)
            queryParams.AddRange(otherSubgroups.Select(sg => $"subgroup={Uri.EscapeDataString(sg)}"));

        var otherCmdbStatuses = viewModel.SelectedCmdbStatuses.Where(s => s != value).ToArray();
        if (otherCmdbStatuses.Length > 0)
            queryParams.AddRange(otherCmdbStatuses.Select(s => $"cmdbStatus={Uri.EscapeDataString(s)}"));

        var otherCmdbGroups = viewModel.SelectedCmdbGroups.Where(g => g != value).ToArray();
        if (otherCmdbGroups.Length > 0)
            queryParams.AddRange(otherCmdbGroups.Select(g => $"parent={Uri.EscapeDataString(g)}"));

        var otherUserGroups = viewModel.SelectedUserGroups.Where(ug => ug != value).ToArray();
        if (otherUserGroups.Length > 0)
            queryParams.AddRange(otherUserGroups.Select(ug => $"userGroup={Uri.EscapeDataString(ug)}"));

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        return $"/products{queryString}";
    }

    // GET: Products/Edit/{fipsid}
    public async Task<IActionResult> ProductEdit(string fipsid)
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
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", categoryValuesDuration) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", categoryValuesDuration) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", categoryValuesDuration) ?? new List<CategoryValue>();

            // Note: Users API call removed since contacts section was removed

            // Note: Product contacts removed since contacts section was removed

            // Find current category values
            var currentPhase = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
            var currentGroup = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Group", StringComparison.OrdinalIgnoreCase) == true);
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
                var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", categoryValuesDuration) ?? new List<CategoryValue>();
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
                var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", categoryValuesDuration) ?? new List<CategoryValue>();
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
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", categoryValuesDuration) ?? new List<CategoryValue>();
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
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", categoryValuesDuration) ?? new List<CategoryValue>();
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
}
