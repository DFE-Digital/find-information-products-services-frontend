using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FipsFrontend.Models
{

    // Reporting-specific Product Contact - simplified version for reporting functionality
    public class ReportingProductContact
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserEmail { get; set; } = string.Empty;
        
        [Required]
        public int ProductId { get; set; }
        
        public string ProductName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
    }

    // Performance Metric Definition - configurable metrics from CMS
    public class PerformanceMetric
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public ReportingCycle Cycle { get; set; }
        
        public bool IsEnabled { get; set; } = true;
        
        [Required]
        public string DataType { get; set; } = string.Empty; // "number", "percentage", "text", "boolean"
        
        public string? Conditions { get; set; } // JSON conditions for when to show this metric
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int SortOrder { get; set; } = 0;
    }

    // Milestone Definition
    public class Milestone
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime DueDate { get; set; }
        
        [Required]
        public MilestoneStatus Status { get; set; }
        
        [Required]
        public string Owner { get; set; } = string.Empty;
        
        [Required]
        public int ProductId { get; set; }
        
        public string ProductName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public string? Category { get; set; } // "strategic", "flagship", "mission"
        
        // Navigation properties
        public List<MilestoneUpdate> Updates { get; set; } = new();
    }

    // Milestone Updates
    public class MilestoneUpdate
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int MilestoneId { get; set; }
        
        [Required]
        public string UpdateText { get; set; } = string.Empty;
        
        public RagStatus? RagStatus { get; set; }
        
        public string? Risks { get; set; }
        
        public string? Issues { get; set; }
        
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Milestone Milestone { get; set; } = null!;
    }

    // Performance Metric Values - actual reported values
    public class PerformanceMetricValue
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int MetricId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        public string ProductName { get; set; } = string.Empty;
        
        [Required]
        public DateTime ReportingPeriod { get; set; }
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
        
        [Required]
        public string ReportedBy { get; set; } = string.Empty;
        
        public string CreatedBy { get; set; } = string.Empty;
        
        public string UpdatedBy { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public PerformanceMetric Metric { get; set; } = null!;
    }

    // Reporting Period Status
    public class ReportingPeriod
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        public string ProductName { get; set; } = string.Empty;
        
        [Required]
        public DateTime PeriodStart { get; set; }
        
        [Required]
        public DateTime PeriodEnd { get; set; }
        
        [Required]
        public ReportingCycle Cycle { get; set; }
        
        public ReportingStatus Status { get; set; } = ReportingStatus.NotStarted;
        
        public DateTime DueDate { get; set; }
        
        public DateTime? SubmittedAt { get; set; }
        
        public string? SubmittedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int CompletedMetrics { get; set; } = 0;
        
        public int TotalMetrics { get; set; } = 0;
    }

    // Audit Log for all changes
    public class ReportingAuditLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string EntityType { get; set; } = string.Empty; // "Milestone", "PerformanceMetricValue", etc.
        
        [Required]
        public int EntityId { get; set; }
        
        [Required]
        public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted"
        
        [Required]
        public string ChangedBy { get; set; } = string.Empty;
        
        public string? OldValues { get; set; } // JSON of old values
        
        public string? NewValues { get; set; } // JSON of new values
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? Notes { get; set; }
    }

    // Reminder Log
    public class ReportingReminder
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ReportingPeriodId { get; set; }
        
        [Required]
        public string UserEmail { get; set; } = string.Empty;
        
        [Required]
        public ReminderType Type { get; set; }
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        public bool IsSent { get; set; } = false;
        
        public string? NotificationId { get; set; } // GOV.UK Notify reference
        
        // Navigation properties
        public ReportingPeriod ReportingPeriod { get; set; } = null!;
    }

    // Enums
    public enum ReportingCycle
    {
        Weekly = 1,
        Monthly = 2,
        Quarterly = 3,
        SixMonthly = 4,
        Annual = 5
    }

    public enum MilestoneStatus
    {
        Open = 1,
        OnTrack = 2,
        AtRisk = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum RagStatus
    {
        Red = 1,
        Amber = 2,
        Green = 3
    }

    public enum ReportingStatus
    {
        NotStarted = 1,
        InProgress = 2,
        Complete = 3,
        Submitted = 4,
        Late = 5,
        Overdue = 6
    }

    public enum ReminderType
    {
        Due = 1,
        Late = 2,
        Overdue = 3
    }

    // View Models for UI
    public class ReportingDashboardViewModel : BaseViewModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public List<ReportingPeriodViewModel> ActivePeriods { get; set; } = new();
        public List<ReportingPeriodViewModel> UpcomingPeriods { get; set; } = new();
        public List<ReportingPeriodViewModel> SubmittedPeriods { get; set; } = new();
        public List<MilestoneViewModel> ActiveMilestones { get; set; } = new();
        public int TotalProducts { get; set; }
        public int CompletedReports { get; set; }
        public int OverdueReports { get; set; }
    }

    public class ReportingPeriodViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public ReportingCycle Cycle { get; set; }
        public ReportingStatus Status { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? SubmittedBy { get; set; }
        public int CompletedMetrics { get; set; }
        public int TotalMetrics { get; set; }
        public double CompletionPercentage => TotalMetrics > 0 ? (double)CompletedMetrics / TotalMetrics * 100 : 0;
        public string StatusDisplay => Status switch
        {
            ReportingStatus.NotStarted => "Not started",
            ReportingStatus.InProgress => "In progress",
            ReportingStatus.Complete => "Complete",
            ReportingStatus.Late => "Late",
            ReportingStatus.Overdue => "Overdue",
            _ => "Unknown"
        };
    }

    public class ServiceReportViewModel : BaseViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public ReportingPeriodViewModel CurrentPeriod { get; set; } = new();
        public List<PerformanceMetricViewModel> PerformanceMetrics { get; set; } = new();
        public List<MilestoneViewModel> Milestones { get; set; } = new();
        public bool CanSubmit => PerformanceMetrics.All(m => m.IsCompleted) && CurrentPeriod.Status != ReportingStatus.Submitted;
    }

    public class PerformanceMetricViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportingCycle Cycle { get; set; }
        public string DataType { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string? CurrentNotes { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class MilestoneViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public MilestoneStatus Status { get; set; }
        public string Owner { get; set; } = string.Empty;
        public string? Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MilestoneUpdateViewModel> Updates { get; set; } = new();
        public string StatusDisplay => Status switch
        {
            MilestoneStatus.Open => "Open",
            MilestoneStatus.OnTrack => "On track",
            MilestoneStatus.AtRisk => "At risk",
            MilestoneStatus.Completed => "Completed",
            MilestoneStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };
    }

    public class MilestoneUpdateViewModel
    {
        public int Id { get; set; }
        public string UpdateText { get; set; } = string.Empty;
        public RagStatus? RagStatus { get; set; }
        public string? Risks { get; set; }
        public string? Issues { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string RagStatusDisplay => RagStatus switch
        {
            Models.RagStatus.Red => "Red",
            Models.RagStatus.Amber => "Amber", 
            Models.RagStatus.Green => "Green",
            _ => "Not set"
        };
    }

    // Additional View Models
    public class ServicesReportViewModel : BaseViewModel
    {
        public List<ReportingPeriodViewModel> ActivePeriods { get; set; } = new();
        public List<ReportingPeriodViewModel> UpcomingPeriods { get; set; } = new();
        public List<ReportingPeriodViewModel> SubmittedPeriods { get; set; } = new();
    }

    public class MetricUpdateViewModel : BaseViewModel
    {
        public int MetricId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string MetricTitle { get; set; } = string.Empty;
        public string MetricDescription { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public DateTime ReportingPeriod { get; set; }
        public string? CurrentValue { get; set; }
        public string? CurrentNotes { get; set; }
        
        [Required(ErrorMessage = "Value is required")]
        public string NewValue { get; set; } = string.Empty;
        
        public string? NewNotes { get; set; }
        
        // Additional properties for the view
        public bool IsCompleted { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class MilestonesViewModel : BaseViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public List<MilestoneViewModel> Milestones { get; set; } = new();
    }

    public class CreateMilestoneViewModel : BaseViewModel
    {
        public int ProductId { get; set; }
        
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }
        
        [Required(ErrorMessage = "Owner is required")]
        [StringLength(255, ErrorMessage = "Owner cannot exceed 255 characters")]
        public string Owner { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string? Category { get; set; }
    }

    public class AddMilestoneUpdateViewModel : BaseViewModel
    {
        [Required(ErrorMessage = "Update text is required")]
        [StringLength(2000, ErrorMessage = "Update text cannot exceed 2000 characters")]
        public string UpdateText { get; set; } = string.Empty;
        
        public RagStatus? RagStatus { get; set; }
        
        [StringLength(1000, ErrorMessage = "Risks cannot exceed 1000 characters")]
        public string? Risks { get; set; }
        
        [StringLength(1000, ErrorMessage = "Issues cannot exceed 1000 characters")]
        public string? Issues { get; set; }
    }
}
