using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing - roles will be added later
public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;
    private readonly CmsApiService _cmsApiService;
    private readonly IOptimizedCmsApiService _optimizedCmsApiService;
    private readonly IConfiguration _configuration;

    public AdminController(ILogger<AdminController> logger, CmsApiService cmsApiService, IOptimizedCmsApiService optimizedCmsApiService, IConfiguration configuration)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _optimizedCmsApiService = optimizedCmsApiService;
        _configuration = configuration;
    }

    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache for 5 minutes
    public async Task<IActionResult> Index()
    {
        try
        {
            // Get cache durations from configuration
            var homeDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Home", 15));
            var categoryTypesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryTypes", 15));
            
            // Get published products count
            var publishedCount = await _optimizedCmsApiService.GetProductsCountAsync(homeDuration);
            
            // Get published category types count
            var categoryTypes = await _optimizedCmsApiService.GetCategoryTypesAsync(categoryTypesDuration);
            var categoryTypesCount = categoryTypes?.Count ?? 0;
            
            var viewModel = new AdminViewModel
            {
                PublishedProductsCount = publishedCount,
                CategoryTypesCount = categoryTypesCount,
                PageTitle = "Admin dashboard",
                PageDescription = "Manage products, categories and system settings"
            };

            ViewData["ActiveNav"] = "admin";
            // Note: For other admin pages, set ViewData["ActiveNavItem"] to the appropriate value:
            // - "manage-products" for /admin/products
            // - "manage-categories" for /admin/categories  
            // - "manage-users" for /admin/users
            // - "export-data" for /admin/export
            // - "use-data-api" for /admin/api
            // - "cache-management" for /admin/cache
            // - "system-performance" for /admin/performance
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            var viewModel = new AdminViewModel
            {
                PublishedProductsCount = 0,
                CategoryTypesCount = 0,
                PageTitle = "Admin dashboard",
                PageDescription = "Manage products, categories and system settings"
            };
            ViewData["ActiveNav"] = "admin";
            return View(viewModel);
        }
    }

    // GET: Admin/Product/Create
    public async Task<IActionResult> ProductCreate()
    {
        try
        {
            // Get Group and Phase category values for the form
            var categoryValuesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", categoryValuesDuration) ?? new List<CategoryValue>();
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", categoryValuesDuration) ?? new List<CategoryValue>();

            // Find Demand phase and set it as selected by default
            var demandPhase = phaseValues.FirstOrDefault(cv => cv.Name.Equals("Demand", StringComparison.OrdinalIgnoreCase));

            var viewModel = new ProductCreateViewModel
            {
                AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList(),
                AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList(),
                SelectedPhaseId = demandPhase?.Id
            };

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product creation form");
            var viewModel = new ProductCreateViewModel();
            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            return View(viewModel);
        }
    }

    // POST: Admin/Product/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductCreate(ProductCreateViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Reload group and phase values for the form
                var reloadDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
                var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", reloadDuration) ?? new List<CategoryValue>();
                var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", reloadDuration) ?? new List<CategoryValue>();
                model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
                model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();

                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-products";
                return View(model);
            }

            // Prepare category values list
            var categoryValuesList = new List<int>();
            
            // Add the selected group if provided (and not empty string)
            if (model.SelectedGroupId.HasValue && model.SelectedGroupId.Value > 0)
            {
                categoryValuesList.Add(model.SelectedGroupId.Value);
            }
            
            // Add the selected phase if provided
            if (model.SelectedPhaseId.HasValue && model.SelectedPhaseId.Value > 0)
            {
                categoryValuesList.Add(model.SelectedPhaseId.Value);
            }

            // Prepare the product data for CMS API
            var productData = new
            {
                data = new
                {
                    title = model.Title,
                    short_description = model.ShortDescription,
                    long_description = model.LongDescription,
                    state = model.State, // Defaults to "Active"
                    category_values = categoryValuesList,
                    publishedAt = (DateTime?)null // Keep as draft [[memory:8564229]]
                }
            };

            // Create the product via CMS API
            var createdProduct = await _cmsApiService.PostAsync<ApiResponse<Product>>("products", productData);

            if (createdProduct?.Data != null)
            {
                TempData["SuccessMessage"] = $"Product '{model.Title}' has been created successfully.";
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to create the product. Please try again.");
                
                // Reload group and phase values for the form
                var errorReloadDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
                var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", errorReloadDuration) ?? new List<CategoryValue>();
                var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", errorReloadDuration) ?? new List<CategoryValue>();
                model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
                model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();

                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-products";
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {Title}", model.Title);
            ModelState.AddModelError("", "An error occurred while creating the product. Please try again.");
            
            // Reload group and phase values for the form
            var catchReloadDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", catchReloadDuration) ?? new List<CategoryValue>();
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", catchReloadDuration) ?? new List<CategoryValue>();
            model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
            model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList();

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            return View(model);
        }
    }
}
