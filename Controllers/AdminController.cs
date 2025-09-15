using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FipsFrontend.Services;
using FipsFrontend.Models;
using System.Text.Json;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class AdminController : Controller
{
    private readonly CmsApiService _cmsApiService;
    private readonly ISecurityService _securityService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(CmsApiService cmsApiService, ISecurityService securityService, ILogger<AdminController> logger)
    {
        _cmsApiService = cmsApiService;
        _securityService = securityService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Products()
    {
        // Check if user has admin permissions
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            _logger.LogWarning("User {UserId} attempted to access admin products without permission", 
                _securityService.GetUserId(HttpContext));
            return Forbid();
        }

        try
        {
            // Get all products using the CMS API - optimized to return only fields needed for admin table
            var products = await _cmsApiService.GetAsync<List<Product>>("products?fields[0]=id&fields[1]=title&fields[2]=short_description&fields[3]=state&fields[4]=fips_id&fields[5]=publishedAt&fields[6]=createdAt");
            
            var viewModel = new AdminProductsViewModel
            {
                Products = products ?? new List<Product>(),
                UserEmail = User.Identity?.Name ?? "Unknown"
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products for admin interface");
            TempData["Error"] = "Failed to load products. Please try again.";
            return View(new AdminProductsViewModel { Products = new List<Product>() });
        }
    }

    [HttpGet]
    public IActionResult CreateProduct()
    {
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            return Forbid();
        }

        return View(new ProductFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(ProductFormViewModel model)
    {
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var productData = new
            {
                data = new
                {
                    fips_id = model.FipsId,
                    title = model.Title,
                    cmdb_sys_id = model.CmdbSysId,
                    short_description = model.ShortDescription,
                    long_description = model.LongDescription,
                    product_url = model.ProductUrl,
                    state = model.State,
                    publishedAt = (string?)null // Keep as draft per user preference
                }
            };

            var result = await _cmsApiService.PostAsync<Product>("products", productData);
            
            if (result != null)
            {
                TempData["Success"] = "Product created successfully.";
                return RedirectToAction(nameof(Products));
            }
            else
            {
                TempData["Error"] = "Failed to create product.";
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            TempData["Error"] = "An error occurred while creating the product.";
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            return Forbid();
        }

        try
        {
            var product = await _cmsApiService.GetAsync<Product>($"products/{id}?populate=*");
            
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Products));
            }

            var viewModel = new ProductFormViewModel
            {
                Id = product.Id,
                FipsId = long.TryParse(product.FipsId, out var fipsId) ? fipsId : null,
                Title = product.Title,
                CmdbSysId = product.CmdbSysId,
                ShortDescription = product.ShortDescription,
                LongDescription = product.LongDescription,
                ProductUrl = product.ProductUrl,
                State = product.State
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} for editing", id);
            TempData["Error"] = "Failed to load product for editing.";
            return RedirectToAction(nameof(Products));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, ProductFormViewModel model)
    {
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var productData = new
            {
                data = new
                {
                    fips_id = model.FipsId,
                    title = model.Title,
                    cmdb_sys_id = model.CmdbSysId,
                    short_description = model.ShortDescription,
                    long_description = model.LongDescription,
                    product_url = model.ProductUrl,
                    state = model.State,
                    publishedAt = model.PublishedAt // Allow publishing if explicitly set
                }
            };

            var result = await _cmsApiService.PutAsync<Product>($"products/{id}", productData);
            
            if (result != null)
            {
                TempData["Success"] = "Product updated successfully.";
                return RedirectToAction(nameof(Products));
            }
            else
            {
                TempData["Error"] = "Failed to update product.";
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            TempData["Error"] = "An error occurred while updating the product.";
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            return Forbid();
        }

        try
        {
            var success = await _cmsApiService.DeleteAsync($"products/{id}");
            
            if (success)
            {
                TempData["Success"] = "Product deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete product.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            TempData["Error"] = "An error occurred while deleting the product.";
        }

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishProduct(int id)
    {
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            return Forbid();
        }

        try
        {
            var productData = new
            {
                data = new
                {
                    publishedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            };

            var result = await _cmsApiService.PutAsync<Product>($"products/{id}", productData);
            
            if (result != null)
            {
                TempData["Success"] = "Product published successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to publish product.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing product {ProductId}", id);
            TempData["Error"] = "An error occurred while publishing the product.";
        }

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpublishProduct(int id)
    {
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            return Forbid();
        }

        try
        {
            var productData = new
            {
                data = new
                {
                    publishedAt = (string?)null
                }
            };

            var result = await _cmsApiService.PutAsync<Product>($"products/{id}", productData);
            
            if (result != null)
            {
                TempData["Success"] = "Product unpublished successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to unpublish product.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing product {ProductId}", id);
            TempData["Error"] = "An error occurred while unpublishing the product.";
        }

        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!_securityService.CanAccessResource(HttpContext, "admin"))
        {
            return Forbid();
        }

        return View();
    }
}
