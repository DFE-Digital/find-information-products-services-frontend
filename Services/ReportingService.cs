using FipsFrontend.Models;
using FipsFrontend.Data;
using Microsoft.EntityFrameworkCore;

namespace FipsFrontend.Services
{
    public interface IReportingService
    {
        Task<ReportingDashboardViewModel> GetDashboardAsync(string userEmail);
        Task<List<ReportingPeriodViewModel>> GetReportingPeriodsAsync(string userEmail);
        Task<ServiceReportViewModel> GetServiceReportAsync(int productId, string userEmail);
        Task<List<PerformanceMetricViewModel>> GetPerformanceMetricsAsync(int productId, DateTime reportingPeriod);
        Task<List<MilestoneViewModel>> GetMilestonesAsync(int productId);
        Task<bool> UpdateMetricValueAsync(int metricId, int productId, DateTime reportingPeriod, string value, string notes, string reportedBy);
        Task<bool> CreateMilestoneAsync(string title, string description, DateTime dueDate, string owner, int productId, string productName, string? category);
        Task<bool> UpdateMilestoneAsync(int milestoneId, MilestoneStatus status, string updatedBy);
        Task<bool> AddMilestoneUpdateAsync(int milestoneId, string updateText, RagStatus? ragStatus, string? risks, string? issues, string updatedBy);
        Task<bool> SubmitReportAsync(int productId, DateTime reportingPeriod, string submittedBy);
        Task<List<ReportingPeriodViewModel>> GetDueReportsAsync();
        Task<List<ReportingPeriodViewModel>> GetLateReportsAsync();
        Task<List<ReportingPeriodViewModel>> GetOverdueReportsAsync();
        Task<bool> IsUserProductContactAsync(string userEmail, int productId);
        Task<List<ReportingProductContact>> GetUserProductsAsync(string userEmail);
        Task LogAuditAsync(string entityType, int entityId, string action, string changedBy, string? oldValues = null, string? newValues = null, string? notes = null);
    }

    public class ReportingService : IReportingService
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(ReportingDbContext context, ILogger<ReportingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReportingDashboardViewModel> GetDashboardAsync(string userEmail)
        {
            var userProducts = await GetUserProductsAsync(userEmail);
            var productIds = userProducts.Select(p => p.ProductId).ToList();

            var activePeriods = await _context.ReportingPeriods
                .Where(rp => productIds.Contains(rp.ProductId) && rp.Status == ReportingStatus.InProgress)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy
                })
                .ToListAsync();

            var upcomingPeriods = await _context.ReportingPeriods
                .Where(rp => productIds.Contains(rp.ProductId) && rp.Status == ReportingStatus.NotStarted)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy
                })
                .ToListAsync();

            var submittedPeriods = await _context.ReportingPeriods
                .Where(rp => productIds.Contains(rp.ProductId) && rp.Status == ReportingStatus.Submitted)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy
                })
                .ToListAsync();

            return new ReportingDashboardViewModel
            {
                UserEmail = userEmail,
                ActivePeriods = activePeriods,
                UpcomingPeriods = upcomingPeriods,
                SubmittedPeriods = submittedPeriods
            };
        }

        public async Task<List<ReportingPeriodViewModel>> GetReportingPeriodsAsync(string userEmail)
        {
            var userProducts = await GetUserProductsAsync(userEmail);
            var productIds = userProducts.Select(p => p.ProductId).ToList();

            return await _context.ReportingPeriods
                .Where(rp => productIds.Contains(rp.ProductId))
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy
                })
                .ToListAsync();
        }

        public async Task<ServiceReportViewModel> GetServiceReportAsync(int productId, string userEmail)
        {
            if (!await IsUserProductContactAsync(userEmail, productId))
            {
                throw new UnauthorizedAccessException("User is not authorized to access this product's reports");
            }

            var product = await _context.ReportingProductContacts
                .Where(pc => pc.ProductId == productId)
                .Select(pc => new { pc.ProductName })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                throw new ArgumentException("Product not found");
            }

            var currentPeriod = await _context.ReportingPeriods
                .Where(rp => rp.ProductId == productId && rp.Status == ReportingStatus.InProgress)
                .OrderByDescending(rp => rp.PeriodStart)
                .FirstOrDefaultAsync();

            var metrics = await _context.PerformanceMetrics
                .Where(pm => pm.IsEnabled)
                .OrderBy(pm => pm.Title)
                .ToListAsync();

            var metricViewModels = new List<PerformanceMetricViewModel>();
            foreach (var metric in metrics)
            {
                var currentValue = await _context.PerformanceMetricValues
                    .Where(pmv => pmv.MetricId == metric.Id && pmv.ProductId == productId && pmv.ReportingPeriod == currentPeriod.PeriodStart)
                    .FirstOrDefaultAsync();

                var viewModel = new PerformanceMetricViewModel
                {
                    Id = metric.Id,
                    Title = metric.Title,
                    Description = metric.Description,
                    DataType = metric.DataType,
                    Cycle = metric.Cycle,
                    CurrentValue = currentValue?.Value,
                    CurrentNotes = currentValue?.Notes,
                    LastUpdated = currentValue?.UpdatedAt,
                    LastUpdatedBy = currentValue?.UpdatedBy,
                    IsCompleted = currentValue != null
                };

                metricViewModels.Add(viewModel);
            }

            var milestones = await _context.Milestones
                .Where(m => m.ProductId == productId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return new ServiceReportViewModel
            {
                ProductId = productId,
                ProductName = product.ProductName,
                CurrentPeriod = currentPeriod != null ? new ReportingPeriodViewModel
                {
                    Id = currentPeriod.Id,
                    ProductId = currentPeriod.ProductId,
                    ProductName = currentPeriod.ProductName,
                    PeriodStart = currentPeriod.PeriodStart,
                    PeriodEnd = currentPeriod.PeriodEnd,
                    Status = currentPeriod.Status,
                    DueDate = currentPeriod.DueDate,
                    SubmittedAt = currentPeriod.SubmittedAt,
                    SubmittedBy = currentPeriod.SubmittedBy
                } : new ReportingPeriodViewModel(),
                PerformanceMetrics = metricViewModels,
                Milestones = milestones.Select(m => new MilestoneViewModel
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    DueDate = m.DueDate,
                    Status = m.Status,
                    Owner = m.Owner,
                    Category = m.Category,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList()
            };
        }

        public async Task<List<PerformanceMetricViewModel>> GetPerformanceMetricsAsync(int productId, DateTime reportingPeriod)
        {
            var metrics = await _context.PerformanceMetrics
                .Where(pm => pm.IsEnabled)
                .OrderBy(pm => pm.Title)
                .ToListAsync();

            var metricViewModels = new List<PerformanceMetricViewModel>();
            foreach (var metric in metrics)
            {
                var currentValue = await _context.PerformanceMetricValues
                    .Where(pmv => pmv.MetricId == metric.Id && pmv.ProductId == productId && pmv.ReportingPeriod == reportingPeriod)
                    .FirstOrDefaultAsync();

                var viewModel = new PerformanceMetricViewModel
                {
                    Id = metric.Id,
                    Title = metric.Title,
                    Description = metric.Description,
                    DataType = metric.DataType,
                    Cycle = metric.Cycle,
                    CurrentValue = currentValue?.Value,
                    CurrentNotes = currentValue?.Notes,
                    LastUpdated = currentValue?.UpdatedAt,
                    LastUpdatedBy = currentValue?.UpdatedBy,
                    IsCompleted = currentValue != null
                };

                metricViewModels.Add(viewModel);
            }

            return metricViewModels;
        }

        public async Task<List<MilestoneViewModel>> GetMilestonesAsync(int productId)
        {
            var milestones = await _context.Milestones
                .Where(m => m.ProductId == productId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return milestones.Select(m => new MilestoneViewModel
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                DueDate = m.DueDate,
                Status = m.Status,
                Owner = m.Owner,
                Category = m.Category,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList();
        }

        public async Task<bool> UpdateMetricValueAsync(int metricId, int productId, DateTime reportingPeriod, string value, string notes, string reportedBy)
        {
            try
            {
                var existingValue = await _context.PerformanceMetricValues
                    .Where(pmv => pmv.MetricId == metricId && pmv.ProductId == productId && pmv.ReportingPeriod == reportingPeriod)
                    .FirstOrDefaultAsync();

                if (existingValue != null)
                {
                    var oldValue = existingValue.Value;
                    var oldNotes = existingValue.Notes;
                    
                    existingValue.Value = value;
                    existingValue.Notes = notes;
                    existingValue.UpdatedAt = DateTime.UtcNow;
                    existingValue.UpdatedBy = reportedBy;

                    await LogAuditAsync("PerformanceMetricValue", existingValue.Id, "Update", reportedBy, 
                        $"Value: {oldValue}, Notes: {oldNotes}", $"Value: {value}, Notes: {notes}");
                }
                else
                {
                    var newValue = new PerformanceMetricValue
                    {
                        MetricId = metricId,
                        ProductId = productId,
                        ReportingPeriod = reportingPeriod,
                        Value = value,
                        Notes = notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = reportedBy,
                        UpdatedBy = reportedBy
                    };

                    _context.PerformanceMetricValues.Add(newValue);
                    await _context.SaveChangesAsync();

                    await LogAuditAsync("PerformanceMetricValue", newValue.Id, "Create", reportedBy, null, 
                        $"Value: {value}, Notes: {notes}");
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metric value for metric {MetricId}, product {ProductId}", metricId, productId);
                return false;
            }
        }

        public async Task<bool> CreateMilestoneAsync(string title, string description, DateTime dueDate, string owner, int productId, string productName, string? category)
        {
            try
            {
                var milestone = new Milestone
                {
                    Title = title,
                    Description = description,
                    DueDate = dueDate,
                    Owner = owner,
                    ProductId = productId,
                    ProductName = productName,
                    Category = category,
                    Status = MilestoneStatus.Open,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Milestones.Add(milestone);
                await _context.SaveChangesAsync();

                await LogAuditAsync("Milestone", milestone.Id, "Create", owner, null, 
                    $"Title: {title}, DueDate: {dueDate}, Category: {category}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating milestone for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> UpdateMilestoneAsync(int milestoneId, MilestoneStatus status, string updatedBy)
        {
            try
            {
                var milestone = await _context.Milestones.FindAsync(milestoneId);
                if (milestone == null) return false;

                var oldStatus = milestone.Status;
                milestone.Status = status;
                milestone.UpdatedAt = DateTime.UtcNow;

                await LogAuditAsync("Milestone", milestoneId, "StatusUpdate", updatedBy, 
                    $"Status: {oldStatus}", $"Status: {status}");

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating milestone {MilestoneId}", milestoneId);
                return false;
            }
        }

        public async Task<bool> AddMilestoneUpdateAsync(int milestoneId, string updateText, RagStatus? ragStatus, string? risks, string? issues, string updatedBy)
        {
            try
            {
                var update = new MilestoneUpdate
                {
                    MilestoneId = milestoneId,
                    UpdateText = updateText,
                    RagStatus = ragStatus,
                    Risks = risks,
                    Issues = issues,
                    UpdatedBy = updatedBy,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MilestoneUpdates.Add(update);
                await _context.SaveChangesAsync();

                await LogAuditAsync("MilestoneUpdate", update.Id, "Create", updatedBy, null, 
                    $"UpdateText: {updateText}, RagStatus: {ragStatus}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding milestone update for milestone {MilestoneId}", milestoneId);
                return false;
            }
        }

        public async Task<bool> SubmitReportAsync(int productId, DateTime reportingPeriod, string submittedBy)
        {
            try
            {
                var period = await _context.ReportingPeriods
                    .Where(rp => rp.ProductId == productId && rp.PeriodStart == reportingPeriod)
                    .FirstOrDefaultAsync();

                if (period == null) return false;

                period.Status = ReportingStatus.Submitted;
                period.SubmittedAt = DateTime.UtcNow;
                period.SubmittedBy = submittedBy;

                await LogAuditAsync("ReportingPeriod", period.Id, "Submit", submittedBy, 
                    $"Status: {ReportingStatus.InProgress}", $"Status: {ReportingStatus.Submitted}");

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting report for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<List<ReportingPeriodViewModel>> GetDueReportsAsync()
        {
            var dueDate = DateTime.UtcNow.AddDays(1); // Due tomorrow
            return await _context.ReportingPeriods
                .Where(rp => rp.Status == ReportingStatus.InProgress && rp.DueDate <= dueDate)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy
                })
                .ToListAsync();
        }

        public async Task<List<ReportingPeriodViewModel>> GetLateReportsAsync()
        {
            var lateDate = DateTime.UtcNow.AddDays(-5); // 5 days overdue
            return await _context.ReportingPeriods
                .Where(rp => rp.Status == ReportingStatus.InProgress && rp.DueDate <= lateDate)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy
                })
                .ToListAsync();
        }

        public async Task<List<ReportingPeriodViewModel>> GetOverdueReportsAsync()
        {
            var overdueDate = DateTime.UtcNow.AddDays(-10); // 10 days overdue
            return await _context.ReportingPeriods
                .Where(rp => rp.Status == ReportingStatus.InProgress && rp.DueDate <= overdueDate)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy
                })
                .ToListAsync();
        }

        public async Task<bool> IsUserProductContactAsync(string userEmail, int productId)
        {
            return await _context.ReportingProductContacts
                .AnyAsync(pc => pc.UserEmail == userEmail && pc.ProductId == productId && pc.IsActive);
        }

        public async Task<List<ReportingProductContact>> GetUserProductsAsync(string userEmail)
        {
            return await _context.ReportingProductContacts
                .Where(pc => pc.UserEmail == userEmail && pc.IsActive)
                .ToListAsync();
        }

        public async Task LogAuditAsync(string entityType, int entityId, string action, string changedBy, string? oldValues = null, string? newValues = null, string? notes = null)
        {
            var auditLog = new ReportingAuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                ChangedBy = changedBy,
                OldValues = oldValues,
                NewValues = newValues,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReportingAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}