using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;
using FipsFrontend.Helpers;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class ContactController : Controller
{
    private readonly ILogger<ContactController> _logger;
    private readonly CmsApiService _cmsApiService;
    private readonly IAirtableService _airtableService;

    public ContactController(ILogger<ContactController> logger, CmsApiService cmsApiService, IAirtableService airtableService)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _airtableService = airtableService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var pageContact = await _cmsApiService.GetAsync<ApiResponse<PageContact>>("page-contact");
            var viewModel = new ContactViewModel
            {
                PageContent = pageContact?.Data
            };
            
            // Process markdown content in the controller
            if (viewModel.PageContent != null)
            {
                _logger.LogInformation("Processing markdown content. Body length: {BodyLength}, RelatedContent length: {RelatedLength}", 
                    viewModel.PageContent.Body?.Length ?? 0, 
                    viewModel.PageContent.RelatedContent?.Length ?? 0);
                
                viewModel.ProcessedBody = GovUkMarkdownHelper.ToGovUkHtml(viewModel.PageContent.Body ?? "");
                viewModel.ProcessedRelatedContent = GovUkMarkdownHelper.ToGovUkHtml(viewModel.PageContent.RelatedContent ?? "");
                
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
            _logger.LogError(ex, "Error loading contact page");
            var viewModel = new ContactViewModel
            {
                PageContent = null
            };
            return View(viewModel);
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackSubmissionModel model)
    {
        try
        {
            _logger.LogInformation("SubmitFeedback endpoint called");
            
            if (model == null)
            {
                _logger.LogWarning("SubmitFeedback called with null model");
                return Json(new { success = false, message = "Invalid request" });
            }
            
            if (string.IsNullOrWhiteSpace(model.FeedbackFormInput))
            {
                _logger.LogWarning("SubmitFeedback called with empty feedback");
                return Json(new { success = false, message = "Feedback cannot be empty" });
            }

            // Get the referring page URL
            var pageUrl = Request.Headers["Referer"].FirstOrDefault() ?? "Unknown";
            var service = "FIPS";
            
            // Get the signed-in user's email
            var userEmail = User?.Identity?.Name ?? User?.FindFirst("email")?.Value ?? User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            _logger.LogInformation("Feedback received: {Feedback}, Page: {PageUrl}, Service: {Service}, UserEmail: {UserEmail}", 
                model.FeedbackFormInput, pageUrl, service, userEmail);

            // Submit to Airtable
            var success = await _airtableService.SubmitFeedbackAsync(model.FeedbackFormInput, pageUrl, service, userEmail);

            if (success)
            {
                _logger.LogInformation("Feedback submitted to Airtable successfully");
                return Json(new { success = true, message = "Feedback submitted successfully" });
            }
            else
            {
                _logger.LogError("Failed to submit feedback to Airtable");
                return Json(new { success = false, message = "An error occurred while processing your feedback" });
            }
        }
        catch (Exception ex)
        {
             Console.WriteLine(ex.Message);
            _logger.LogError(ex, "Error processing feedback submission");
            return Json(new { success = false, message = "An error occurred while processing your feedback" });
        }
    }
}
