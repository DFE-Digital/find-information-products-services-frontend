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

    public async Task<IActionResult> Index()
    {
        try
        {
            // No caching for admin operations
            var publishedCount = await _optimizedCmsApiService.GetProductsCountAsync(null);
            
            // Get published category types count (no caching for admin)
            var categoryTypes = await _optimizedCmsApiService.GetCategoryTypesAsync(null);
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
            // No caching for admin operations
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", null) ?? new List<CategoryValue>();
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", null) ?? new List<CategoryValue>();

            // Find Demand phase and set it as selected by default
            var demandPhase = phaseValues.FirstOrDefault(cv => cv.Name.Equals("Demand", StringComparison.OrdinalIgnoreCase));

            var viewModel = new ProductCreateViewModel
            {
                AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList(),
                AvailablePhases = phaseValues.Where(cv => cv.Enabled).OrderBy(cv => cv.SortOrder ?? int.MaxValue).ToList(),
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
                model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).OrderBy(cv => cv.SortOrder ?? int.MaxValue).ToList();

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
                    long_description = model.ShortDescription, // Set long description same as short description
                    state = model.State, // Defaults to "Active"
                    category_values = categoryValuesList,
                    publishedAt = (DateTime?)null // Keep as draft [[memory:8564229]]
                }
            };

            // Create the product via CMS API
            var createdProduct = await _cmsApiService.PostAsync<ApiResponse<Product>>("products", productData);

            if (createdProduct?.Data != null)
            {
                // Redirect to confirmation page with product details
                return RedirectToAction("ProductCreateConfirmation", new { 
                    productId = createdProduct.Data.Id,
                    title = model.Title,
                    referenceNumber = createdProduct.Data.FipsId ?? $"PRD-{createdProduct.Data.Id:D6}"
                });
            }
            else
            {
                ModelState.AddModelError("", "Failed to create the product. Please try again.");
                
                // Reload group and phase values for the form
                var errorReloadDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryValues", 15));
                var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", errorReloadDuration) ?? new List<CategoryValue>();
                var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", errorReloadDuration) ?? new List<CategoryValue>();
                model.AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList();
                model.AvailablePhases = phaseValues.Where(cv => cv.Enabled).OrderBy(cv => cv.SortOrder ?? int.MaxValue).ToList();

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

    // GET: Admin/ProductCreateConfirmation
    public IActionResult ProductCreateConfirmation(int productId, string title, string referenceNumber)
    {
        try
        {
            var viewModel = new ProductCreateConfirmationViewModel
            {
                ProductId = productId,
                ProductTitle = title,
                ReferenceNumber = referenceNumber
            };

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product creation confirmation page");
            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            return RedirectToAction("Index");
        }
    }

    // GET: Admin/ProductManage
    public async Task<IActionResult> ProductManage(int page = 1, string? search = null, string? state = null)
    {
        try
        {
            const int pageSize = 20;
            
            // Get products with search and pagination using dedicated admin API (no caching for admin)
            var (products, totalCount) = await _optimizedCmsApiService.GetProductsForAdminAsync(
                page: page,
                pageSize: pageSize,
                searchQuery: search,
                stateFilter: state,
                cacheDuration: null); // No caching for admin operations

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Get available states for the filter dropdown (no caching for admin)
            var availableStates = await _optimizedCmsApiService.GetAvailableStates(null);

            // Get state counts for the summary cards (no caching for admin)
            var stateCounts = await GetStateCounts(null);

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = totalCount;
            ViewData["SearchQuery"] = search;
            ViewData["SelectedState"] = state;
            ViewData["AvailableStates"] = availableStates;
            ViewData["StateCounts"] = stateCounts;
            
            return View(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product management page");
            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            return View(new List<Product>());
        }
    }

    // POST: Admin/ProductDelete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductDelete(int id)
    {
        try
        {
            // Get the product first to preserve its FIPS_ID
            var product = await _optimizedCmsApiService.GetProductByIdAsync(id, null);
            
            // Instead of deleting, set the product state to "Deleted" (preserve FIPS_ID to prevent regeneration)
            var updateData = new
            {
                data = new
                {
                    state = "Deleted",
                    fips_id = product?.FipsId // Explicitly preserve the FIPS_ID if available
                }
            };
            var result = await _cmsApiService.PutAsync<Product>($"products/{id}", updateData);

            if (result != null)
            {
                TempData["SuccessMessage"] = "Product has been marked as deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete the product. Please try again.";
            }

            return RedirectToAction("ProductManage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {ProductId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the product. Please try again.";
            return RedirectToAction("ProductManage");
        }
    }

    // GET: Admin/ProductDetail/{id}
    public async Task<IActionResult> ProductDetail(string id, string? subPage = null)
    {
        try
        {
            // Get the specific product using FIPS ID (no caching for admin)
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            
            // If subPage is "categories", redirect to ProductManageCategories
            if (subPage == "categories")
            {
                return RedirectToAction("ProductManageCategories", new { id });
            }
            
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product detail page for product {ProductId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the product.";
            return RedirectToAction("ProductManage");
        }
    }

    // GET: Admin/ProductStateChange/{id}
    public async Task<IActionResult> ProductStateChange(string id)
    {
        try
        {
            // Get the specific product using FIPS ID (no caching for admin)
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            var currentState = product.State ?? "Unknown";
            var validTransitions = GetValidStateTransitions(currentState);

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            ViewData["CurrentState"] = currentState;
            ViewData["ValidTransitions"] = validTransitions;
            ViewData["ProductTitle"] = product.Title;
            
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product state change page for product {FipsId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the product.";
            return RedirectToAction("ProductManage");
        }
    }

    // POST: Admin/ProductStateChange
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductStateChange(string id, string newState)
    {
        try
        {
            // Get the product first to validate current state
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            // Validate state transition
            var currentState = product.State;
            var validTransitions = GetValidStateTransitions(currentState);
            
            if (!validTransitions.Contains(newState))
            {
                TempData["ErrorMessage"] = $"Cannot change state from '{currentState}' to '{newState}'. Valid transitions are: {string.Join(", ", validTransitions)}";
                return RedirectToAction("ProductManage");
            }

            // Prepare update data - handle title modification based on state changes
            object updateData;
            string? modifiedTitle = null;
            
            if (newState == "Deleted" && currentState != "Deleted")
            {
                // Adding "(DELETED)" to title when setting to Deleted
                modifiedTitle = product.Title?.Contains(" (DELETED)") == true 
                    ? product.Title 
                    : $"{product.Title} (DELETED)";
            }
            else if (currentState == "Deleted" && newState != "Deleted")
            {
                // Removing "(DELETED)" from title when changing from Deleted
                modifiedTitle = product.Title?.Replace(" (DELETED)", "").Trim();
            }
            
            if (modifiedTitle != null)
            {
                updateData = new
                {
                    data = new
                    {
                        state = newState,
                        title = modifiedTitle,
                        fips_id = product.FipsId // Explicitly preserve the FIPS_ID
                    }
                };
            }
            else
            {
                updateData = new
                {
                    data = new
                    {
                        state = newState,
                        fips_id = product.FipsId // Explicitly preserve the FIPS_ID
                    }
                };
            }
            var result = await _cmsApiService.PutAsync<Product>($"products/{product.DocumentId}", updateData);

            if (result != null)
            {
                var message = $"Product state successfully changed from '{currentState}' to '{newState}'.";
                
                // Add information about title modification
                if (newState == "Deleted" && currentState != "Deleted" && product.Title?.Contains(" (DELETED)") != true)
                {
                    message += " The '(DELETED)' tag has been added to the product title.";
                }
                else if (currentState == "Deleted" && newState != "Deleted" && product.Title?.Contains(" (DELETED)") == true)
                {
                    message += " The '(DELETED)' tag has been removed from the product title.";
                }
                
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product state. Please try again.";
            }

            return RedirectToAction("ProductManage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing product state: {FipsId} to {NewState}", id, newState);
            TempData["ErrorMessage"] = "An error occurred while updating the product state. Please try again.";
            return RedirectToAction("ProductManage");
        }
    }

    // GET: Admin/ProductEditTitle/{id}
    public async Task<IActionResult> ProductEditTitle(string id)
    {
        try
        {
            // No caching for admin operations
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            ViewData["ProductTitle"] = product.Title;
            
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product title edit page for product {Id}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the product.";
            return RedirectToAction("ProductManage");
        }
    }

    // POST: Admin/ProductEditTitle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductEditTitle(string id, string title)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Product title is required.";
                return RedirectToAction("ProductEditTitle", new { id });
            }

            // Get the product first to get the documentId
            // No caching for admin operations
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            // Update the product title (preserve FIPS_ID to prevent regeneration)
            var updateData = new
            {
                data = new
                {
                    title = title.Trim(),
                    fips_id = product.FipsId // Explicitly preserve the FIPS_ID
                }
            };
            var result = await _cmsApiService.PutAsync<Product>($"products/{product.DocumentId}", updateData);

            if (result != null)
            {
                TempData["SuccessMessage"] = $"Product title successfully updated to '{title.Trim()}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product title. Please try again.";
            }

            return RedirectToAction("ProductDetail", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product title: {Id} to {Title}", id, title);
            TempData["ErrorMessage"] = "An error occurred while updating the product title. Please try again.";
            return RedirectToAction("ProductEditTitle", new { id });
        }
    }

    // GET: Admin/ProductEditShortDescription/{id}
    public async Task<IActionResult> ProductEditShortDescription(string id)
    {
        try
        {
            // No caching for admin operations
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            ViewData["ProductTitle"] = product.Title;
            
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product short description edit page for product {FipsId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the product.";
            return RedirectToAction("ProductManage");
        }
    }

    // POST: Admin/ProductEditShortDescription
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductEditShortDescription(string id, string shortDescription)
    {
        try
        {
            // Get the product first to get the documentId
            // No caching for admin operations
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            // Update the product short description (preserve FIPS_ID to prevent regeneration)
            var updateData = new
            {
                data = new
                {
                    short_description = shortDescription?.Trim() ?? "",
                    fips_id = product.FipsId // Explicitly preserve the FIPS_ID
                }
            };
            var result = await _cmsApiService.PutAsync<Product>($"products/{product.DocumentId}", updateData);

            if (result != null)
            {
                TempData["SuccessMessage"] = "Product short description successfully updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product short description. Please try again.";
            }

            return RedirectToAction("ProductDetail", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product short description: {FipsId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the product short description. Please try again.";
            return RedirectToAction("ProductEditShortDescription", new { id });
        }
    }

    // GET: Admin/ProductEditLongDescription/{id}
    public async Task<IActionResult> ProductEditLongDescription(string id)
    {
        try
        {
            // No caching for admin operations
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            ViewData["ProductTitle"] = product.Title;
            
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product long description edit page for product {FipsId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the product.";
            return RedirectToAction("ProductManage");
        }
    }

    // POST: Admin/ProductEditLongDescription
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductEditLongDescription(string id, string longDescription)
    {
        try
        {
            // Get the product first to get the documentId
            // No caching for admin operations
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            // Update the product long description (preserve FIPS_ID to prevent regeneration)
            var updateData = new
            {
                data = new
                {
                    long_description = longDescription?.Trim() ?? "",
                    fips_id = product.FipsId // Explicitly preserve the FIPS_ID
                }
            };
            var result = await _cmsApiService.PutAsync<Product>($"products/{product.DocumentId}", updateData);

            if (result != null)
            {
                TempData["SuccessMessage"] = "Product long description successfully updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product long description. Please try again.";
            }

            return RedirectToAction("ProductDetail", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product long description: {FipsId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the product long description. Please try again.";
            return RedirectToAction("ProductEditLongDescription", new { id });
        }
    }

    // GET: Admin/ProductManageCategories/{id}
    public async Task<IActionResult> ProductManageCategories(string id)
    {
        try
        {
            // No caching for admin operations
            // No caching for admin operations
            
            // Get the product
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            // Get all available category values for each category type
            var phaseValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Phase", null) ?? new List<CategoryValue>();
            var groupValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Group", null) ?? new List<CategoryValue>();
            var channelValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Channel", null) ?? new List<CategoryValue>();
            var typeValues = await _optimizedCmsApiService.GetCategoryValuesForFilter("Type", null) ?? new List<CategoryValue>();

            // Get current category values for the product
            var currentPhase = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
            var currentGroup = product.CategoryValues?.FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Group", StringComparison.OrdinalIgnoreCase) == true);
            var currentChannels = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Channel", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();
            var currentTypes = product.CategoryValues?.Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CategoryValue>();

            var viewModel = new ProductCategoriesViewModel
            {
                Product = product,
                AvailablePhases = phaseValues.Where(cv => cv.Enabled).ToList(),
                AvailableGroups = groupValues.Where(cv => cv.Enabled).ToList(),
                AvailableChannels = channelValues.Where(cv => cv.Enabled).ToList(),
                AvailableTypes = typeValues.Where(cv => cv.Enabled).ToList(),
                SelectedPhaseId = currentPhase?.Id,
                SelectedGroupId = currentGroup?.Id,
                SelectedChannelIds = currentChannels.Select(c => c.Id).ToList(),
                SelectedTypeIds = currentTypes.Select(c => c.Id).ToList(),
                PageTitle = $"Manage categories: {product.Title}",
                PageDescription = $"Manage categories and values for {product.Title}"
            };

            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-products";
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product categories management page for product {FipsId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the product categories.";
            return RedirectToAction("ProductManage");
        }
    }

    // POST: Admin/ProductManageCategories
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductManageCategories(string id, int? selectedPhaseId, int? selectedGroupId, List<int>? selectedChannelIds, List<int>? selectedTypeIds)
    {
        try
        {
            // Get the product first to get the documentId
            // No caching for admin operations
            var product = await _optimizedCmsApiService.GetProductByFipsIdAsync(id, null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("ProductManage");
            }

            // Prepare category values list
            var categoryValuesList = new List<int>();
            
            if (selectedPhaseId.HasValue && selectedPhaseId.Value > 0)
            {
                categoryValuesList.Add(selectedPhaseId.Value);
            }
            if (selectedGroupId.HasValue && selectedGroupId.Value > 0)
            {
                categoryValuesList.Add(selectedGroupId.Value);
            }
            if (selectedChannelIds != null)
            {
                categoryValuesList.AddRange(selectedChannelIds.Where(id => id > 0));
            }
            if (selectedTypeIds != null)
            {
                categoryValuesList.AddRange(selectedTypeIds.Where(id => id > 0));
            }

            // Update the product categories (preserve FIPS_ID to prevent regeneration)
            var updateData = new
            {
                data = new
                {
                    category_values = categoryValuesList,
                    fips_id = product.FipsId // Explicitly preserve the FIPS_ID
                }
            };
            var result = await _cmsApiService.PutAsync<Product>($"products/{product.DocumentId}", updateData);

            if (result != null)
            {
                TempData["SuccessMessage"] = "Product categories successfully updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product categories. Please try again.";
            }

            return RedirectToAction("ProductDetail", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product categories: {FipsId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the product categories. Please try again.";
            return RedirectToAction("ProductManageCategories", new { id });
        }
    }

    private async Task<Dictionary<string, int>> GetStateCounts(TimeSpan? cacheDuration)
    {
        try
        {
            var stateCounts = new Dictionary<string, int>();
            var availableStates = await _optimizedCmsApiService.GetAvailableStates(cacheDuration);

            foreach (var state in availableStates)
            {
                var count = await _optimizedCmsApiService.GetProductsCountAsync(cacheDuration, state);
                stateCounts[state] = count;
            }

            return stateCounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting state counts");
            return new Dictionary<string, int>();
        }
    }

    private static string[] GetValidStateTransitions(string currentState)
    {
        return currentState switch
        {
            "New" => new[] { "Active", "Rejected", "Deleted" },
            "Active" => new[] { "Rejected", "Deleted" },
            "Rejected" => new[] { "Active", "Deleted" },
            "Deleted" => new[] { "Active", "Rejected" },
            _ => new string[0]
        };
    }
}
