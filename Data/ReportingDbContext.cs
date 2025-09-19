using Microsoft.EntityFrameworkCore;
using FipsFrontend.Models;

namespace FipsFrontend.Data
{
    public class ReportingDbContext : DbContext
    {
        public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options)
        {
        }

        public DbSet<ReportingProductContact> ReportingProductContacts { get; set; }
        public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
        public DbSet<Milestone> Milestones { get; set; }
        public DbSet<MilestoneUpdate> MilestoneUpdates { get; set; }
        public DbSet<PerformanceMetricValue> PerformanceMetricValues { get; set; }
        public DbSet<ReportingPeriod> ReportingPeriods { get; set; }
        public DbSet<ReportingAuditLog> ReportingAuditLogs { get; set; }
        public DbSet<ReportingReminder> ReportingReminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ReportingProductContact
            modelBuilder.Entity<ReportingProductContact>(entity =>
            {
                entity.HasIndex(e => new { e.UserEmail, e.ProductId }).IsUnique();
                entity.Property(e => e.UserEmail).HasMaxLength(255);
                entity.Property(e => e.ProductName).HasMaxLength(500);
            });

            // Configure PerformanceMetric
            modelBuilder.Entity<PerformanceMetric>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.DataType).HasMaxLength(50);
                entity.Property(e => e.Conditions).HasMaxLength(2000);
            });

            // Configure Milestone
            modelBuilder.Entity<Milestone>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Owner).HasMaxLength(255);
                entity.Property(e => e.ProductName).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(100);
            });

            // Configure MilestoneUpdate
            modelBuilder.Entity<MilestoneUpdate>(entity =>
            {
                entity.Property(e => e.UpdateText).HasMaxLength(2000);
                entity.Property(e => e.Risks).HasMaxLength(1000);
                entity.Property(e => e.Issues).HasMaxLength(1000);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                
                entity.HasOne(d => d.Milestone)
                    .WithMany(p => p.Updates)
                    .HasForeignKey(d => d.MilestoneId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PerformanceMetricValue
            modelBuilder.Entity<PerformanceMetricValue>(entity =>
            {
                entity.Property(e => e.Value).HasMaxLength(500);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.ProductName).HasMaxLength(500);
                entity.Property(e => e.ReportedBy).HasMaxLength(255);
                
                entity.HasIndex(e => new { e.MetricId, e.ProductId, e.ReportingPeriod }).IsUnique();
                
                entity.HasOne(d => d.Metric)
                    .WithMany()
                    .HasForeignKey(d => d.MetricId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ReportingPeriod
            modelBuilder.Entity<ReportingPeriod>(entity =>
            {
                entity.Property(e => e.ProductName).HasMaxLength(500);
                entity.Property(e => e.SubmittedBy).HasMaxLength(255);
                
                entity.HasIndex(e => new { e.ProductId, e.PeriodStart, e.Cycle }).IsUnique();
            });

            // Configure ReportingAuditLog
            modelBuilder.Entity<ReportingAuditLog>(entity =>
            {
                entity.Property(e => e.EntityType).HasMaxLength(100);
                entity.Property(e => e.Action).HasMaxLength(50);
                entity.Property(e => e.ChangedBy).HasMaxLength(255);
                entity.Property(e => e.OldValues).HasMaxLength(4000);
                entity.Property(e => e.NewValues).HasMaxLength(4000);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure ReportingReminder
            modelBuilder.Entity<ReportingReminder>(entity =>
            {
                entity.Property(e => e.UserEmail).HasMaxLength(255);
                entity.Property(e => e.NotificationId).HasMaxLength(100);
                
                entity.HasOne(d => d.ReportingPeriod)
                    .WithMany()
                    .HasForeignKey(d => d.ReportingPeriodId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed some initial performance metrics
            modelBuilder.Entity<PerformanceMetric>().HasData(
                new PerformanceMetric
                {
                    Id = 1,
                    Title = "Customer satisfaction score (%)",
                    Description = "Percentage of users satisfied with the service",
                    Cycle = ReportingCycle.Monthly,
                    DataType = "percentage",
                    IsEnabled = true,
                    SortOrder = 1
                },
                new PerformanceMetric
                {
                    Id = 2,
                    Title = "Uptime (%)",
                    Description = "Percentage of time the service was available",
                    Cycle = ReportingCycle.Monthly,
                    DataType = "percentage",
                    IsEnabled = true,
                    SortOrder = 2
                },
                new PerformanceMetric
                {
                    Id = 3,
                    Title = "Monthly active users",
                    Description = "Number of unique users who accessed the service",
                    Cycle = ReportingCycle.Monthly,
                    DataType = "number",
                    IsEnabled = true,
                    SortOrder = 3
                },
                new PerformanceMetric
                {
                    Id = 4,
                    Title = "Transactions",
                    Description = "Number of transactions completed",
                    Cycle = ReportingCycle.Monthly,
                    DataType = "number",
                    IsEnabled = true,
                    SortOrder = 4
                },
                new PerformanceMetric
                {
                    Id = 5,
                    Title = "Incident Count",
                    Description = "Number of incidents reported",
                    Cycle = ReportingCycle.Monthly,
                    DataType = "number",
                    IsEnabled = true,
                    SortOrder = 5
                },
                new PerformanceMetric
                {
                    Id = 6,
                    Title = "Cost per Transaction (£)",
                    Description = "Average cost per transaction",
                    Cycle = ReportingCycle.Monthly,
                    DataType = "currency",
                    IsEnabled = true,
                    SortOrder = 6
                }
            );
        }
    }
}
