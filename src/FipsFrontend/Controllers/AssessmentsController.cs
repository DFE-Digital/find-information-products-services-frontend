using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class AssessmentsController : Controller
{
    private readonly ILogger<AssessmentsController> _logger;
    private readonly IServiceAssessmentsService _assessmentsService;
    private readonly IConfiguration _configuration;

    public AssessmentsController(ILogger<AssessmentsController> logger, IServiceAssessmentsService assessmentsService, IConfiguration configuration)
    {
        _logger = logger;
        _assessmentsService = assessmentsService;
        _configuration = configuration;
    }

    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache for 5 minutes
    public async Task<IActionResult> Index(int page = 1, int pageSize = 25, string? search = null, string? type = null, string? phase = null, string? status = null)
    {
        try
        {
            // Get cache durations from configuration
            var assessmentsDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Assessments", 15));
            
            // Build filters
            var filters = new Dictionary<string, string[]>();
            
            if (!string.IsNullOrEmpty(type))
            {
                filters["type"] = new[] { type };
            }
            
            if (!string.IsNullOrEmpty(phase))
            {
                filters["phase"] = new[] { phase };
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                filters["status"] = new[] { status };
            }

            // Get assessments data
            var (assessments, totalCount) = await _assessmentsService.GetAssessmentsSummaryAsync(
                page, 
                pageSize, 
                search, 
                filters.Any() ? filters : null,
                assessmentsDuration
            );

            // Get filter options
            var availableTypes = await _assessmentsService.GetAvailableTypesAsync(assessmentsDuration);
            var availablePhases = await _assessmentsService.GetAvailablePhasesAsync(assessmentsDuration);
            var availableStatuses = await _assessmentsService.GetAvailableStatusesAsync(assessmentsDuration);

            // Build selected filters for display
            var selectedFilters = new Dictionary<string, string[]>();
            if (!string.IsNullOrEmpty(type))
                selectedFilters["Type"] = new[] { type };
            if (!string.IsNullOrEmpty(phase))
                selectedFilters["Phase"] = new[] { phase };
            if (!string.IsNullOrEmpty(status))
                selectedFilters["Status"] = new[] { status };

            var viewModel = new AssessmentsViewModel
            {
                Assessments = assessments,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                SearchQuery = search,
                TypeFilter = type,
                PhaseFilter = phase,
                StatusFilter = status,
                AvailableTypes = availableTypes,
                AvailablePhases = availablePhases,
                AvailableStatuses = availableStatuses,
                SelectedFilters = selectedFilters,
                PageTitle = "Service assessments"
            };

            ViewData["ActiveNav"] = "assessments";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assessments");
            var viewModel = new AssessmentsViewModel
            {
                Assessments = new List<AssessmentSummary>(),
                TotalCount = 0,
                CurrentPage = page,
                PageSize = pageSize,
                SearchQuery = search,
                TypeFilter = type,
                PhaseFilter = phase,
                StatusFilter = status,
                AvailableTypes = new List<string>(),
                AvailablePhases = new List<string>(),
                AvailableStatuses = new List<string>(),
                SelectedFilters = new Dictionary<string, string[]>(),
                PageTitle = "Service assessments"
            };
            return View(viewModel);
        }
    }

    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache for 5 minutes
    public async Task<IActionResult> Detail(int id)
    {
        try
        {
            var assessmentsDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Assessments", 15));
            var assessment = await _assessmentsService.GetAssessmentByIdAsync(id, assessmentsDuration);

            if (assessment == null)
            {
                return NotFound();
            }

            ViewData["ActiveNav"] = "assessments";
            return View(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assessment detail for ID: {Id}", id);
            return NotFound();
        }
    }
}
