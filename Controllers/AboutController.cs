using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;
using FipsFrontend.Helpers;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class AboutController : Controller
{
    private readonly ILogger<AboutController> _logger;
    private readonly CmsApiService _cmsApiService;

    public AboutController(ILogger<AboutController> logger, CmsApiService cmsApiService)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var pageAbout = await _cmsApiService.GetAsync<ApiResponse<PageAbout>>("page-about");
            var viewModel = new AboutViewModel
            {
                PageContent = pageAbout?.Data
            };
            
            // Process markdown content in the controller
            if (viewModel.PageContent != null)
            {
                _logger.LogInformation("Processing markdown content. Body length: {BodyLength}, RelatedContent length: {RelatedLength}", 
                    viewModel.PageContent.Body?.Length ?? 0, 
                    viewModel.PageContent.RelatedContent?.Length ?? 0);
                
                viewModel.ProcessedBody = GovUkMarkdownHelper.ToGovUkHtml(viewModel.PageContent.Body ?? "");
                viewModel.ProcessedRelatedContent = GovUkMarkdownHelper.ToGovUkPlainList(viewModel.PageContent.RelatedContent ?? "");
                
                _logger.LogInformation("Processed content. ProcessedBody length: {ProcessedBodyLength}, ProcessedRelatedContent length: {ProcessedRelatedLength}", 
                    viewModel.ProcessedBody?.Length ?? 0, 
                    viewModel.ProcessedRelatedContent?.Length ?? 0);
            }
            else
            {
                _logger.LogWarning("PageContent is null - no content to process");
            }
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading about page");
            var viewModel = new AboutViewModel
            {
                PageContent = null
            };
            return View(viewModel);
        }
    }
}
