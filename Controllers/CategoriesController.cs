using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class CategoriesController : Controller
{
    private readonly ILogger<CategoriesController> _logger;
    private readonly CmsApiService _cmsApiService;

    public CategoriesController(ILogger<CategoriesController> logger, CmsApiService cmsApiService)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
    }

    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)] // Cache for 10 minutes
    public async Task<IActionResult> Index()
    {
        try
        {
            // Log the API call
            var categoryTypesUrl = "category-types?filters[publishedAt][$notNull]=true&filters[enabled]=true&sort=sort_order:asc";
            _logger.LogInformation("=== CATEGORY TYPES API CALL ===");
            _logger.LogInformation("URL: {Url}", categoryTypesUrl);
            
            // Get category types first, then populate values separately with proper parent-child relationships (cache for 15 minutes)
            var categoryTypes = await _cmsApiService.GetAllCategoryTypes(TimeSpan.FromMinutes(15)) ?? new List<CategoryType>();

            // Log the raw response
            _logger.LogInformation("=== CATEGORY TYPES API RESPONSE ===");
            _logger.LogInformation("Status: Success, Count: {Count}", categoryTypes.Count);
            if (categoryTypes.Any())
            {
                foreach (var ct in categoryTypes)
                {
                    _logger.LogInformation("Category Type: {Name} (Slug: {Slug}, MultiLevel: {MultiLevel}, Enabled: {Enabled})", 
                        ct.Name, ct.Slug, ct.MultiLevel, ct.Enabled);
                }
            }

            // Fetch values separately for each category type with proper population
            if (categoryTypes.Any())
            {
                foreach (var categoryType in categoryTypes)
                {
                    _logger.LogInformation("=== PROCESSING CATEGORY TYPE: {Name} ===", categoryType.Name);
                    
                    // Get all values for this category type - try different populate approaches
                    var valuesUrl = $"category-values?filters[category_type][slug][$eq]={categoryType.Slug}&filters[publishedAt][$notNull]=true&filters[enabled]=true&populate=*&sort=sort_order:asc";
                    _logger.LogInformation("Values API URL: {Url}", valuesUrl);
                    
                    var valuesResponse = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryValue>>(valuesUrl);
                    
                    // Log the raw response for values
                    _logger.LogInformation("=== VALUES API RESPONSE FOR {Name} ===", categoryType.Name);
                    _logger.LogInformation("Total values returned: {Count}", valuesResponse?.Data?.Count ?? 0);
                    
                    var allValues = valuesResponse?.Data ?? new List<CategoryValue>();
                    
                    // Log each value with its parent/children info
                    foreach (var value in allValues.Take(10)) // Log first 10 values
                    {
                        _logger.LogInformation("Value: {Name} (Slug: {Slug}, Parent: {ParentId}, Children: {ChildrenCount})", 
                            value.Name, value.Slug, value.Parent?.Id, value.Children?.Count ?? 0);
                    }
                    
                    // Log parent-child relationships
                    var rootCount = allValues.Count(v => v.Parent == null);
                    var withParents = allValues.Count(v => v.Parent != null);
                    _logger.LogInformation("Root items: {RootCount}, Items with parents: {WithParents}", rootCount, withParents);
                    
                    // Set the values on the category type
                    categoryType.Values = allValues;
                    
                    // Log the counts for debugging
                    if (categoryType.MultiLevel)
                    {
                        _logger.LogInformation("Multi-level category {Name}: {RootCount} root items", categoryType.Name, rootCount);
                    }
                    else
                    {
                        _logger.LogInformation("Single-level category {Name}: {Count} items", categoryType.Name, allValues.Count);
                    }
                }
            }

            var viewModel = new CategoriesIndexViewModel
            {
                CategoryTypes = categoryTypes ?? new List<CategoryType>()
            };


            ViewData["ActiveNav"] = "categories";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category types");
            var viewModel = new CategoriesIndexViewModel
            {
                CategoryTypes = new List<CategoryType>()
            };
            return View(viewModel);
        }
    }

    public async Task<IActionResult> Detail(string slug)
    {
        try
        {
            if (string.IsNullOrEmpty(slug))
            {
                // No slug provided, show the index page with comprehensive logging
                _logger.LogInformation("=== INDEX PAGE REQUEST (via Detail action) ===");
                
                // Log the API call
                var categoryTypesUrl = "category-types?filters[publishedAt][$notNull]=true&filters[enabled]=true&sort=sort_order:asc";
                _logger.LogInformation("=== CATEGORY TYPES API CALL ===");
                _logger.LogInformation("URL: {Url}", categoryTypesUrl);
                
                // Get category types first, then populate values separately with proper parent-child relationships
                var categoryTypes = await _cmsApiService.GetAllCategoryTypes(TimeSpan.FromMinutes(15)) ?? new List<CategoryType>();

                // Log the raw response
                _logger.LogInformation("=== CATEGORY TYPES API RESPONSE ===");
                _logger.LogInformation("Status: Success, Count: {Count}", categoryTypes?.Count ?? 0);
                if (categoryTypes != null)
                {
                    foreach (var ct in categoryTypes)
                    {
                        _logger.LogInformation("Category Type: {Name} (Slug: {Slug}, MultiLevel: {MultiLevel}, Enabled: {Enabled})", 
                            ct.Name, ct.Slug, ct.MultiLevel, ct.Enabled);
                    }
                }

                // Fetch values separately for each category type with proper population
                if (categoryTypes != null)
                {
                    foreach (var ct in categoryTypes)
                    {
                        _logger.LogInformation("=== PROCESSING CATEGORY TYPE: {Name} ===", ct.Name);
                        
                        // Get all values for this category type - filter for root items only (no parent)
                        var valuesUrl = $"category-values?filters[category_type][slug][$eq]={ct.Slug}&filters[publishedAt][$notNull]=true&filters[enabled]=true&filters[parent][$null]=true&populate[parent]=true&populate[children]=true&sort=sort_order:asc";
                        _logger.LogInformation("Values API URL: {Url}", valuesUrl);
                        
                        var valuesResponse = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryValue>>(valuesUrl);
                        
                        // Log the raw response for values
                        _logger.LogInformation("=== VALUES API RESPONSE FOR {Name} ===", ct.Name);
                        _logger.LogInformation("Total values returned: {Count}", valuesResponse?.Data?.Count ?? 0);
                        
                        var ctValues = valuesResponse?.Data ?? new List<CategoryValue>();
                        
                        // Log each value with its parent/children info
                        foreach (var value in ctValues.Take(10)) // Log first 10 values
                        {
                            _logger.LogInformation("Value: {Name} (Slug: {Slug}, Parent: {ParentId}, Children: {ChildrenCount})", 
                                value.Name, value.Slug, value.Parent?.DocumentId, value.Children?.Count ?? 0);
                        }
                        
                        // Log all items with null parent specifically
                        var nullParentItems = ctValues.Where(v => v.Parent == null).ToList();
                        _logger.LogInformation("Items with null parent: {Count}", nullParentItems.Count);
                        foreach (var item in nullParentItems)
                        {
                            _logger.LogInformation("Null parent item: {Name} (Slug: {Slug})", item.Name, item.Slug);
                        }
                        
                        // Log parent-child relationships
                        var rootCount = ctValues.Count(v => v.Parent == null);
                        var withParents = ctValues.Count(v => v.Parent != null);
                        _logger.LogInformation("Root items: {RootCount}, Items with parents: {WithParents}", rootCount, withParents);
                        
                        // Set the values on the category type
                        ct.Values = ctValues;
                        
                        // Log the counts for debugging
                        if (ct.MultiLevel)
                        {
                            _logger.LogInformation("Multi-level category {Name}: {RootCount} root items", ct.Name, rootCount);
                        }
                        else
                        {
                            _logger.LogInformation("Single-level category {Name}: {Count} items", ct.Name, ctValues.Count);
                        }
                    }
                }

                var indexViewModel = new CategoriesIndexViewModel
                {
                    CategoryTypes = categoryTypes ?? new List<CategoryType>()
                };

                return View("Index", indexViewModel);
            }

            // Parse the slug path to determine category type and level
            var slugParts = slug.Split('/');
            var categoryTypeSlug = slugParts[0];
            
            _logger.LogInformation("Processing category slug: {Slug}, parts: {Parts}, categoryTypeSlug: {CategoryTypeSlug}", 
                slug, string.Join(", ", slugParts), categoryTypeSlug);
            
            // Get the category type by slug
            var categoryTypeResponse = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryType>>(
                $"category-types?filters[slug][$eq]={categoryTypeSlug}&filters[publishedAt][$notNull]=true"
            );

            var categoryType = categoryTypeResponse?.Data?.FirstOrDefault();
            if (categoryType == null)
            {
                _logger.LogWarning("Category type not found for slug: {Slug}", categoryTypeSlug);
                return NotFound();
            }
            
            _logger.LogInformation("Found category type: {Name} (Slug: {Slug})", categoryType.Name, categoryType.Slug);

            var viewModel = new CategoriesDetailViewModel
            {
                CategoryType = categoryType,
                CurrentSlug = slug,
                BreadcrumbPath = slug
            };

            // Get all published and enabled category values for this type with full population
            var allValuesUrl = $"category-values?filters[category_type][slug][$eq]={categoryTypeSlug}&filters[publishedAt][$notNull]=true&filters[enabled]=true&filters[parent][$null]=true&populate[parent]=true&populate[children]=true&sort=sort_order:asc";
            _logger.LogInformation("=== DETAIL VALUES API CALL ===");
            _logger.LogInformation("URL: {Url}", allValuesUrl);
            
            var allValuesResponse = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryValue>>(allValuesUrl);
            var allValues = allValuesResponse?.Data ?? new List<CategoryValue>();
            
            _logger.LogInformation("=== DETAIL VALUES API RESPONSE ===");
            _logger.LogInformation("Found {Count} total category values for {CategoryTypeSlug}", allValues.Count, categoryTypeSlug);
            
            // Log the raw JSON response to see what we're actually getting
            _logger.LogInformation("=== RAW API RESPONSE DEBUG ===");
            if (allValuesResponse?.Data != null && allValuesResponse.Data.Any())
            {
                var firstValue = allValuesResponse.Data.First();
                _logger.LogInformation("First value JSON structure: Name={Name}, Slug={Slug}, Parent={Parent}, Children={Children}", 
                    firstValue.Name, firstValue.Slug, firstValue.Parent?.Id, firstValue.Children?.Count);
            }
            
            // Log all values with their relationships
            _logger.LogInformation("=== ALL VALUES FOR DETAIL {CategoryTypeSlug} ===", categoryTypeSlug);
            foreach (var value in allValues.Take(20)) // Log first 20 values
            {
                _logger.LogInformation("Value: {Name} (Slug: {Slug}, Parent: {ParentId}, Children: {ChildrenCount})", 
                    value.Name, value.Slug, value.Parent?.DocumentId, value.Children?.Count ?? 0);
            }
            
            // Log all items with null parent specifically
            var detailNullParentItems = allValues.Where(v => v.Parent == null).ToList();
            _logger.LogInformation("Items with null parent: {Count}", detailNullParentItems.Count);
            foreach (var item in detailNullParentItems)
            {
                _logger.LogInformation("Null parent item: {Name} (Slug: {Slug})", item.Name, item.Slug);
            }

            if (slugParts.Length == 1)
            {
                // Top level - show root categories (no parent) for multi-level, or all items for single-level
                if (categoryType.MultiLevel)
                {
                    viewModel.CategoryValues = allValues.Where(v => v.Parent == null).ToList();
                    _logger.LogInformation("Multi-level category: showing {Count} root items", viewModel.CategoryValues.Count);
                    
                    // Debug: Log each root item being shown
                    foreach (var item in viewModel.CategoryValues)
                    {
                        _logger.LogInformation("Root item: {Name} (Slug: {Slug}, Parent: {Parent}, Children: {ChildrenCount})", 
                            item.Name, item.Slug, item.Parent?.DocumentId, item.Children?.Count ?? 0);
                    }
                }
                else
                {
                    viewModel.CategoryValues = allValues.ToList();
                    _logger.LogInformation("Single-level category: showing {Count} items", viewModel.CategoryValues.Count);
                }
                viewModel.PageTitle = categoryType.Name;
                viewModel.PageDescription = categoryType.Description ?? $"Browse products in {categoryType.Name}";
            }
            else
            {
                // Multi-level navigation - need to get ALL values (not just root items) to find children
                var allValuesForNavigationUrl = $"category-values?filters[category_type][slug][$eq]={categoryTypeSlug}&filters[publishedAt][$notNull]=true&filters[enabled]=true&populate[parent]=true&populate[children]=true&sort=sort_order:asc";
                _logger.LogInformation("=== MULTI-LEVEL NAVIGATION API CALL ===");
                _logger.LogInformation("URL: {Url}", allValuesForNavigationUrl);
                
                var allValuesForNavigationResponse = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryValue>>(allValuesForNavigationUrl);
                var allValuesForNavigation = allValuesForNavigationResponse?.Data ?? new List<CategoryValue>();
                
                _logger.LogInformation("Found {Count} total values for multi-level navigation", allValuesForNavigation.Count);
                
                // Debug: Log first 10 values from allValuesForNavigation to see their parent status
                _logger.LogInformation("=== MULTI-LEVEL NAVIGATION VALUES DEBUG ===");
                foreach (var value in allValuesForNavigation.Take(10))
                {
                    _logger.LogInformation("Value: {Name} (Slug: {Slug}, Parent: {ParentId}, ParentType: {ParentType})", 
                        value.Name, value.Slug, value.Parent?.DocumentId, value.Parent?.GetType().Name);
                }
                
                // Multi-level navigation - navigate through the path to find the current parent
                CategoryValue? currentParent = null;
                var breadcrumbNames = new List<string>();
                
                for (int i = 1; i < slugParts.Length; i++)
                {
                    var currentSlug = slugParts[i];
                    var parentId = currentParent?.DocumentId;
                    
                    // For the first sub-level (i=1), look for root items in allValues (which has only root items)
                    // For deeper levels, look for children of the current parent in allValuesForNavigation
                    var searchCollection = (i == 1) ? allValues : allValuesForNavigation;
                    
                    
                    var foundCategory = searchCollection.FirstOrDefault(v => 
                        v.Slug == currentSlug && 
                        ((i == 1 && v.Parent == null) || (i > 1 && v.Parent?.DocumentId == parentId)));
                    
                    if (foundCategory == null)
                    {
                        _logger.LogWarning("Category not found: {Slug} with parent {ParentId} (level {Level})", currentSlug, parentId, i);
                        return NotFound();
                    }
                    
                    currentParent = foundCategory;
                    breadcrumbNames.Add(foundCategory.Name);
                    
                }
                
                if (currentParent != null)
                {
                    viewModel.ParentCategory = currentParent;
                    
                    // Get children directly from API instead of filtering all records
                    _logger.LogInformation("Current parent DocumentId: {DocumentId}", currentParent.DocumentId);
                    var childrenUrl = $"category-values?filters[parent][documentId][$eq]={currentParent.DocumentId}&filters[publishedAt][$notNull]=true&filters[enabled]=true&populate=*&sort=sort_order:asc";
                    _logger.LogInformation("=== CHILDREN API CALL ===");
                    _logger.LogInformation("URL: {Url}", childrenUrl);
                    
                    var childrenResponse = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryValue>>(childrenUrl);
                    var children = childrenResponse?.Data ?? new List<CategoryValue>();
                    _logger.LogInformation("Found {Count} children for parent {ParentName}", children.Count, currentParent.Name);
                    viewModel.CategoryValues = children;
                    
                    if (currentParent.Parent != null)
                    {
                        // Parent is a string DocumentId, so compare with DocumentId
                        viewModel.SiblingCategories = allValuesForNavigation.Where(v => v.Parent?.DocumentId == currentParent.Parent.DocumentId && v.DocumentId != currentParent.DocumentId).ToList();
                    }
                    
                    viewModel.PageTitle = $"{string.Join(" - ", breadcrumbNames)}";
                    viewModel.Group = categoryType.Name;
                    viewModel.PageDescription = $"Browse products in {currentParent.Name}";
                }
                else
                {
                    return NotFound();
                }
            }

            ViewData["ActiveNav"] = "categories";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category detail for slug: {Slug}", slug);
            return NotFound();
        }
    }

    public IActionResult Filter(string categoryType, string slug)
    {
        // Redirect to products page with appropriate filter
        var filterParam = categoryType.ToLower() switch
        {
            "phase" => $"phase={slug}",
            "channel" => $"channel={slug}",
            "type" => $"type={slug}",
            "group" => $"group={slug}",
            "user group" => $"userGroup={slug}",
            _ => $"category={slug}"
        };

        return Redirect($"/products?{filterParam}");
    }
}
