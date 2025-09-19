# FIPS Governance Reporting Feature Setup Guide

## Overview

The governance reporting feature allows product contacts to submit updates to configurable metrics and manage strategic milestones. The feature includes automated email reminders via GOV.UK Notify.

## Quick Start

### 1. Database Setup

The SQLite database will be created automatically when the application starts. For production, update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ReportingDb": "Data Source=reporting.db"
  }
}
```

### 2. GOV.UK Notify Configuration (Optional)

To enable email reminders, configure GOV.UK Notify in `appsettings.json`:

```json
{
  "GOVUKNotify": {
    "ApiKey": "your-actual-api-key-here",
    "Templates": {
      "DueReport": "your-due-report-template-id",
      "LateReport": "your-late-report-template-id", 
      "OverdueReport": "your-overdue-report-template-id",
      "ReportSubmitted": "your-report-submitted-template-id"
    }
  },
  "BaseUrl": "https://your-frontend-url.com"
}
```

**Note**: The application will run without GOV.UK Notify configured - notifications will simply be disabled.

### 3. Accessing the Reporting Feature

Navigate to the following URLs:

- **Dashboard**: `/reports` - Overview of all reporting periods
- **Services**: `/reports/services` - List of services requiring reports
- **Service Report**: `/reports/services/{productId}` - Individual service reporting
- **Metric Update**: `/reports/services/{productId}/metric/{metricId}` - Update specific metrics
- **Milestones**: `/reports/services/{productId}/milestones` - Manage milestones

## Features

### Performance Metrics
- Configurable metrics defined in CMS
- Support for different data types (number, percentage, text, boolean)
- Configurable reporting cycles (weekly, monthly, quarterly, 6-monthly, annual)
- Conditional display based on product criteria

### Milestones
- Strategic objectives, flagship projects, or missions
- RAG status tracking (Red, Amber, Green)
- Progress updates with notes
- Risk and issue tracking

### Audit Logging
- Complete change tracking
- Who made changes, when, and what changed
- JSON storage of old and new values

### Automated Reminders
- Due reminders (1st of following month)
- Late reminders (5th working day)
- Overdue reminders (10th working day)
- Email notifications via GOV.UK Notify

## Database Schema

The feature uses the following main tables:

- `ReportingProductContacts` - User-product relationships
- `PerformanceMetrics` - Configurable metrics from CMS
- `PerformanceMetricValues` - Actual metric values
- `Milestones` - Strategic milestones
- `MilestoneUpdates` - Progress updates
- `ReportingAuditLogs` - Change tracking
- `ReportingReminders` - Email reminder logs

## CMS Integration

The feature is designed to work with Strapi CMS. Content types should be created for:

- `performance-metric` - Configurable metrics
- `product-contact` - User-product relationships
- `milestone` - Strategic milestones
- `milestone-update` - Progress updates

## Development Notes

- Uses Entity Framework Core with SQLite for local development
- Designed for Azure SQL Database in production
- Follows GOV.UK Design System patterns
- Includes comprehensive error handling and logging
- Supports both authenticated and anonymous access patterns

## Troubleshooting

### GOV.UK Notify Errors
If you see `NotifyAuthException` errors, ensure:
1. The API key is valid and not the placeholder value
2. The API key is a v2 key from GOV.UK Notify
3. The template IDs are correct

### Database Issues
- Ensure the application has write permissions to the database file location
- For production, verify the connection string is correct
- Check Entity Framework migrations are applied

### Missing Data
- Verify product contacts are properly configured
- Check that performance metrics are enabled
- Ensure reporting periods are created correctly
