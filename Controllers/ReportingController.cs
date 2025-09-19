using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Models;
using FipsFrontend.Services;
using FipsFrontend.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Controllers
{
    public class ReportingController : Controller
    {
        private readonly IReportingService _reportingService;
        private readonly INotificationService _notificationService;
        private readonly ReportingDbContext _context;
        private readonly ILogger<ReportingController> _logger;

        public ReportingController(
            IReportingService reportingService,
            INotificationService notificationService,
            ReportingDbContext context,
            ILogger<ReportingController> logger)
        {
            _reportingService = reportingService;
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        // GET: /reports
        public async Task<IActionResult> Dashboard()
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            var viewModel = await _reportingService.GetDashboardAsync(userEmail);
            return View(viewModel);
        }

        // GET: /reports/services
        public async Task<IActionResult> Services()
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            var periods = await _reportingService.GetReportingPeriodsAsync(userEmail);
            
            var viewModel = new ServicesReportViewModel
            {
                ActivePeriods = periods.Where(p => p.Status == ReportingStatus.InProgress).ToList(),
                UpcomingPeriods = periods.Where(p => p.Status == ReportingStatus.NotStarted).ToList(),
                SubmittedPeriods = periods.Where(p => p.Status == ReportingStatus.Complete).Take(5).ToList()
            };
            
            return View(viewModel);
        }

        // GET: /reports/services/{productId}
        public async Task<IActionResult> ServiceReport(int productId)
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            try
            {
                var viewModel = await _reportingService.GetServiceReportAsync(productId, userEmail);
                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // GET: /reports/services/{productId}/metric/{metricId}
        public async Task<IActionResult> MetricUpdate(int productId, int metricId)
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            if (!await _reportingService.IsUserProductContactAsync(userEmail, productId))
            {
                return Forbid();
            }

            var metric = await _context.PerformanceMetrics.FindAsync(metricId);
            if (metric == null)
            {
                return NotFound();
            }

            var product = await _context.ReportingProductContacts
                .FirstOrDefaultAsync(pc => pc.ProductId == productId);

            var currentPeriod = await _context.ReportingPeriods
                .Where(rp => rp.ProductId == productId)
                .OrderByDescending(rp => rp.PeriodStart)
                .FirstOrDefaultAsync();

            var currentPeriodDate = currentPeriod?.PeriodStart ?? DateTime.UtcNow;
            var currentValue = await _context.PerformanceMetricValues
                .FirstOrDefaultAsync(v => v.MetricId == metricId && 
                                        v.ProductId == productId && 
                                        v.ReportingPeriod.Date == currentPeriodDate.Date);

            var viewModel = new MetricUpdateViewModel
            {
                MetricId = metricId,
                ProductId = productId,
                ProductName = product?.ProductName ?? "Unknown Product",
                MetricTitle = metric.Title,
                MetricDescription = metric.Description,
                DataType = metric.DataType,
                ReportingPeriod = currentPeriod?.PeriodStart ?? DateTime.UtcNow,
                CurrentValue = currentValue?.Value,
                CurrentNotes = currentValue?.Notes
            };

            return View(viewModel);
        }

        // POST: /reports/services/{productId}/metric/{metricId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MetricUpdate(int productId, int metricId, MetricUpdateViewModel model)
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            if (!await _reportingService.IsUserProductContactAsync(userEmail, productId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var success = await _reportingService.UpdateMetricValueAsync(
                    metricId, productId, model.ReportingPeriod, model.NewValue, model.NewNotes, userEmail);

                if (success)
                {
                    TempData["SuccessMessage"] = "Metric updated successfully";
                    return RedirectToAction("ServiceReport", new { productId });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update metric. Please try again.");
                }
            }

            // Reload the metric details for the view
            var metric = await _context.PerformanceMetrics.FindAsync(metricId);
            var product = await _context.ReportingProductContacts
                .FirstOrDefaultAsync(pc => pc.ProductId == productId);

            model.MetricTitle = metric?.Title ?? "Unknown Metric";
            model.MetricDescription = metric?.Description ?? "";
            model.DataType = metric?.DataType ?? "text";
            model.ProductName = product?.ProductName ?? "Unknown Product";

            return View(model);
        }

        // GET: /reports/services/{productId}/milestones
        public async Task<IActionResult> Milestones(int productId)
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            if (!await _reportingService.IsUserProductContactAsync(userEmail, productId))
            {
                return Forbid();
            }

            var milestones = await _reportingService.GetMilestonesAsync(productId);
            var product = await _context.ReportingProductContacts
                .FirstOrDefaultAsync(pc => pc.ProductId == productId);

            var viewModel = new MilestonesViewModel
            {
                ProductId = productId,
                ProductName = product?.ProductName ?? "Unknown Product",
                Milestones = milestones
            };

            return View(viewModel);
        }

        // POST: /reports/services/{productId}/milestones/create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMilestone(int productId, CreateMilestoneViewModel model)
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            if (!await _reportingService.IsUserProductContactAsync(userEmail, productId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var product = await _context.ReportingProductContacts
                    .FirstOrDefaultAsync(pc => pc.ProductId == productId);

                var success = await _reportingService.CreateMilestoneAsync(
                    model.Title, model.Description, model.DueDate, model.Owner, 
                    productId, product?.ProductName ?? "Unknown Product", model.Category);

                if (success)
                {
                    TempData["SuccessMessage"] = "Milestone created successfully";
                    return RedirectToAction("Milestones", new { productId });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to create milestone. Please try again.");
                }
            }

            model.ProductId = productId;
            return View(model);
        }

        // POST: /reports/services/{productId}/milestones/{milestoneId}/update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMilestone(int productId, int milestoneId, MilestoneStatus status)
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            if (!await _reportingService.IsUserProductContactAsync(userEmail, productId))
            {
                return Forbid();
            }

            var success = await _reportingService.UpdateMilestoneAsync(milestoneId, status, userEmail);

            if (success)
            {
                TempData["SuccessMessage"] = "Milestone updated successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update milestone. Please try again.";
            }

            return RedirectToAction("Milestones", new { productId });
        }

        // POST: /reports/services/{productId}/milestones/{milestoneId}/add-update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMilestoneUpdate(int productId, int milestoneId, AddMilestoneUpdateViewModel model)
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            if (!await _reportingService.IsUserProductContactAsync(userEmail, productId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var success = await _reportingService.AddMilestoneUpdateAsync(
                    milestoneId, model.UpdateText, model.RagStatus, model.Risks, model.Issues, userEmail);

                if (success)
                {
                    TempData["SuccessMessage"] = "Milestone update added successfully";
                    return RedirectToAction("Milestones", new { productId });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to add milestone update. Please try again.");
                }
            }

            return RedirectToAction("Milestones", new { productId });
        }

        // POST: /reports/services/{productId}/submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(int productId)
        {
            // For now, use a test user email - in production this would come from authentication
            var userEmail = "test.user@education.gov.uk";
            
            if (!await _reportingService.IsUserProductContactAsync(userEmail, productId))
            {
                return Forbid();
            }

            var currentPeriod = await _context.ReportingPeriods
                .Where(rp => rp.ProductId == productId)
                .OrderByDescending(rp => rp.PeriodStart)
                .FirstOrDefaultAsync();

            if (currentPeriod == null)
            {
                TempData["ErrorMessage"] = "No reporting period found for this product.";
                return RedirectToAction("ServiceReport", new { productId });
            }

            var success = await _reportingService.SubmitReportAsync(productId, currentPeriod.PeriodStart, userEmail);

            if (success)
            {
                TempData["SuccessMessage"] = "Report submitted successfully";
                
                // Send confirmation email
                var periodViewModel = new ReportingPeriodViewModel
                {
                    Id = currentPeriod.Id,
                    ProductId = currentPeriod.ProductId,
                    ProductName = currentPeriod.ProductName,
                    PeriodStart = currentPeriod.PeriodStart,
                    PeriodEnd = currentPeriod.PeriodEnd,
                    Cycle = currentPeriod.Cycle,
                    Status = currentPeriod.Status,
                    DueDate = currentPeriod.DueDate,
                    SubmittedAt = currentPeriod.SubmittedAt,
                    SubmittedBy = currentPeriod.SubmittedBy,
                    CompletedMetrics = currentPeriod.CompletedMetrics,
                    TotalMetrics = currentPeriod.TotalMetrics
                };
                
                await _notificationService.SendReportSubmittedConfirmationAsync(userEmail, periodViewModel);
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to submit report. Please try again.";
            }

            return RedirectToAction("ServiceReport", new { productId });
        }
    }

}
