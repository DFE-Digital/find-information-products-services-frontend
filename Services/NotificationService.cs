using FipsFrontend.Models;
using FipsFrontend.Data;
using Microsoft.EntityFrameworkCore;
using Notify.Client;
using Notify.Models;
using Notify.Models.Responses;

namespace FipsFrontend.Services
{
    public interface INotificationService
    {
        Task<bool> SendDueReportReminderAsync(string userEmail, ReportingPeriodViewModel period);
        Task<bool> SendLateReportReminderAsync(string userEmail, ReportingPeriodViewModel period);
        Task<bool> SendOverdueReportReminderAsync(string userEmail, ReportingPeriodViewModel period);
        Task<bool> SendReportSubmittedConfirmationAsync(string userEmail, ReportingPeriodViewModel period);
        Task ProcessRemindersAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly NotificationClient? _notifyClient;

        public NotificationService(ReportingDbContext context, ILogger<NotificationService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;

            var apiKey = _configuration["GOVUKNotify:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_GOVUK_NOTIFY_API_KEY")
            {
                try
                {
                    _notifyClient = new NotificationClient(apiKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize GOV.UK Notify client. Notifications will be disabled.");
                    _notifyClient = null;
                }
            }
            else
            {
                _logger.LogInformation("GOV.UK Notify API key not configured. Notifications will be disabled.");
            }
        }

        public async Task<bool> SendDueReportReminderAsync(string userEmail, ReportingPeriodViewModel period)
        {
            if (_notifyClient == null)
            {
                _logger.LogWarning("GOV.UK Notify client not configured, skipping notification");
                return false;
            }

            try
            {
                var templateId = _configuration["GOVUKNotify:Templates:DueReport"];
                if (string.IsNullOrEmpty(templateId))
                {
                    _logger.LogWarning("Due report template ID not configured");
                    return false;
                }

                var personalisation = new Dictionary<string, dynamic>
                {
                    { "product_name", period.ProductName },
                    { "reporting_period", period.PeriodStart.ToString("MMMM yyyy") },
                    { "due_date", period.DueDate.ToString("dd MMMM yyyy") },
                    { "report_url", $"{_configuration["BaseUrl"]}/reports/services/{period.ProductId}" }
                };

                var response = await _notifyClient.SendEmailAsync(userEmail, templateId, personalisation);
                
                // Log the reminder
                await LogReminderAsync(period.Id, userEmail, ReminderType.Due, response.id);

                _logger.LogInformation("Due report reminder sent to {UserEmail} for product {ProductId}, notification ID: {NotificationId}", 
                    userEmail, period.ProductId, response.id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending due report reminder to {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> SendLateReportReminderAsync(string userEmail, ReportingPeriodViewModel period)
        {
            if (_notifyClient == null)
            {
                _logger.LogWarning("GOV.UK Notify client not configured, skipping notification");
                return false;
            }

            try
            {
                var templateId = _configuration["GOVUKNotify:Templates:LateReport"];
                if (string.IsNullOrEmpty(templateId))
                {
                    _logger.LogWarning("Late report template ID not configured");
                    return false;
                }

                var personalisation = new Dictionary<string, dynamic>
                {
                    { "product_name", period.ProductName },
                    { "reporting_period", period.PeriodStart.ToString("MMMM yyyy") },
                    { "due_date", period.DueDate.ToString("dd MMMM yyyy") },
                    { "report_url", $"{_configuration["BaseUrl"]}/reports/services/{period.ProductId}" }
                };

                var response = await _notifyClient.SendEmailAsync(userEmail, templateId, personalisation);
                
                // Log the reminder
                await LogReminderAsync(period.Id, userEmail, ReminderType.Late, response.id);

                _logger.LogInformation("Late report reminder sent to {UserEmail} for product {ProductId}, notification ID: {NotificationId}", 
                    userEmail, period.ProductId, response.id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending late report reminder to {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> SendOverdueReportReminderAsync(string userEmail, ReportingPeriodViewModel period)
        {
            if (_notifyClient == null)
            {
                _logger.LogWarning("GOV.UK Notify client not configured, skipping notification");
                return false;
            }

            try
            {
                var templateId = _configuration["GOVUKNotify:Templates:OverdueReport"];
                if (string.IsNullOrEmpty(templateId))
                {
                    _logger.LogWarning("Overdue report template ID not configured");
                    return false;
                }

                var personalisation = new Dictionary<string, dynamic>
                {
                    { "product_name", period.ProductName },
                    { "reporting_period", period.PeriodStart.ToString("MMMM yyyy") },
                    { "due_date", period.DueDate.ToString("dd MMMM yyyy") },
                    { "report_url", $"{_configuration["BaseUrl"]}/reports/services/{period.ProductId}" }
                };

                var response = await _notifyClient.SendEmailAsync(userEmail, templateId, personalisation);
                
                // Log the reminder
                await LogReminderAsync(period.Id, userEmail, ReminderType.Overdue, response.id);

                _logger.LogInformation("Overdue report reminder sent to {UserEmail} for product {ProductId}, notification ID: {NotificationId}", 
                    userEmail, period.ProductId, response.id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending overdue report reminder to {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> SendReportSubmittedConfirmationAsync(string userEmail, ReportingPeriodViewModel period)
        {
            if (_notifyClient == null)
            {
                _logger.LogWarning("GOV.UK Notify client not configured, skipping notification");
                return false;
            }

            try
            {
                var templateId = _configuration["GOVUKNotify:Templates:ReportSubmitted"];
                if (string.IsNullOrEmpty(templateId))
                {
                    _logger.LogWarning("Report submitted template ID not configured");
                    return false;
                }

                var personalisation = new Dictionary<string, dynamic>
                {
                    { "product_name", period.ProductName },
                    { "reporting_period", period.PeriodStart.ToString("MMMM yyyy") },
                    { "submitted_date", period.SubmittedAt?.ToString("dd MMMM yyyy") ?? "Unknown" }
                };

                var response = await _notifyClient.SendEmailAsync(userEmail, templateId, personalisation);

                _logger.LogInformation("Report submitted confirmation sent to {UserEmail} for product {ProductId}, notification ID: {NotificationId}", 
                    userEmail, period.ProductId, response.id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending report submitted confirmation to {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task ProcessRemindersAsync()
        {
            try
            {
                // Get due reports (due today)
                var dueReports = await GetDueReportsAsync();
                foreach (var report in dueReports)
                {
                    var contacts = await GetProductContactsAsync(report.ProductId);
                    foreach (var contact in contacts)
                    {
                        var alreadySent = await HasReminderBeenSentAsync(report.Id, contact.UserEmail, ReminderType.Due);
                        if (!alreadySent)
                        {
                            await SendDueReportReminderAsync(contact.UserEmail, report);
                        }
                    }
                }

                // Get late reports (1-5 days overdue)
                var lateReports = await GetLateReportsAsync();
                foreach (var report in lateReports)
                {
                    var contacts = await GetProductContactsAsync(report.ProductId);
                    foreach (var contact in contacts)
                    {
                        var alreadySent = await HasReminderBeenSentAsync(report.Id, contact.UserEmail, ReminderType.Late);
                        if (!alreadySent)
                        {
                            await SendLateReportReminderAsync(contact.UserEmail, report);
                        }
                    }
                }

                // Get overdue reports (5+ days overdue)
                var overdueReports = await GetOverdueReportsAsync();
                foreach (var report in overdueReports)
                {
                    var contacts = await GetProductContactsAsync(report.ProductId);
                    foreach (var contact in contacts)
                    {
                        var alreadySent = await HasReminderBeenSentAsync(report.Id, contact.UserEmail, ReminderType.Overdue);
                        if (!alreadySent)
                        {
                            await SendOverdueReportReminderAsync(contact.UserEmail, report);
                        }
                    }
                }

                _logger.LogInformation("Processed reminders: {DueCount} due, {LateCount} late, {OverdueCount} overdue", 
                    dueReports.Count, lateReports.Count, overdueReports.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminders");
            }
        }

        private async Task<List<ReportingPeriodViewModel>> GetDueReportsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            return await _context.ReportingPeriods
                .Where(rp => rp.DueDate.Date == firstOfMonth && rp.Status == ReportingStatus.NotStarted)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Cycle = rp.Cycle,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy,
                    CompletedMetrics = rp.CompletedMetrics,
                    TotalMetrics = rp.TotalMetrics
                })
                .ToListAsync();
        }

        private async Task<List<ReportingPeriodViewModel>> GetLateReportsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var fifthOfMonth = new DateTime(today.Year, today.Month, 5);

            return await _context.ReportingPeriods
                .Where(rp => rp.DueDate.Date < today && rp.DueDate.Date >= fifthOfMonth && rp.Status == ReportingStatus.InProgress)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Cycle = rp.Cycle,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy,
                    CompletedMetrics = rp.CompletedMetrics,
                    TotalMetrics = rp.TotalMetrics
                })
                .ToListAsync();
        }

        private async Task<List<ReportingPeriodViewModel>> GetOverdueReportsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tenthOfMonth = new DateTime(today.Year, today.Month, 10);

            return await _context.ReportingPeriods
                .Where(rp => rp.DueDate.Date < tenthOfMonth && rp.Status != ReportingStatus.Complete)
                .Select(rp => new ReportingPeriodViewModel
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    ProductName = rp.ProductName,
                    PeriodStart = rp.PeriodStart,
                    PeriodEnd = rp.PeriodEnd,
                    Cycle = rp.Cycle,
                    Status = rp.Status,
                    DueDate = rp.DueDate,
                    SubmittedAt = rp.SubmittedAt,
                    SubmittedBy = rp.SubmittedBy,
                    CompletedMetrics = rp.CompletedMetrics,
                    TotalMetrics = rp.TotalMetrics
                })
                .ToListAsync();
        }

        private async Task<List<ReportingProductContact>> GetProductContactsAsync(int productId)
        {
            return await _context.ReportingProductContacts
                .Where(pc => pc.ProductId == productId && pc.IsActive)
                .ToListAsync();
        }

        private async Task<bool> HasReminderBeenSentAsync(int reportingPeriodId, string userEmail, ReminderType type)
        {
            return await _context.ReportingReminders
                .AnyAsync(r => r.ReportingPeriodId == reportingPeriodId && 
                             r.UserEmail == userEmail && 
                             r.Type == type && 
                             r.IsSent);
        }

        private async Task LogReminderAsync(int reportingPeriodId, string userEmail, ReminderType type, string notificationId)
        {
            try
            {
                var reminder = new ReportingReminder
                {
                    ReportingPeriodId = reportingPeriodId,
                    UserEmail = userEmail,
                    Type = type,
                    NotificationId = notificationId,
                    IsSent = true,
                    SentAt = DateTime.UtcNow
                };

                _context.ReportingReminders.Add(reminder);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging reminder for period {PeriodId}, user {UserEmail}", reportingPeriodId, userEmail);
            }
        }
    }
}
