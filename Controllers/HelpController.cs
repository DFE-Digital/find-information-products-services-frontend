using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;
using FipsFrontend.Helpers;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class HelpController : Controller
{
    private readonly ILogger<HelpController> _logger;
    private readonly CmsApiService _cmsApiService;

    public HelpController(ILogger<HelpController> logger, CmsApiService cmsApiService)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var pageHelp = await _cmsApiService.GetAsync<ApiResponse<PageHelp>>("page-help");
            var viewModel = new HelpViewModel
            {
                PageContent = pageHelp?.Data
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
            _logger.LogError(ex, "Error loading help page");
            var viewModel = new HelpViewModel
            {
                PageContent = null
            };
            return View(viewModel);
        }
    }
}
