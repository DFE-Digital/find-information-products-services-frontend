# FIPS Reporting Feature

This document describes the governance reporting feature implemented for the FIPS platform, inspired by the [DfE Register of Services](https://register-of-services-7c72d99322f5.herokuapp.com/reports) design patterns.

## Overview

The reporting feature allows product contacts to submit monthly performance reports for services they are responsible for. It includes milestone tracking, configurable performance metrics, and automated reminder notifications.

## Features

### 1. Reporting Dashboard
- Overview of all reporting periods (active, upcoming, submitted)
- Status tracking with visual indicators
- Progress monitoring for each service
- Active milestones display

### 2. Service Reports
- Individual service reporting pages
- Organised by categories (Customer Experience, Availability & Performance, etc.)
- Configurable performance metrics
- Milestone management
- Report submission workflow

### 3. Performance Metrics
- Configurable metrics defined in CMS
- Support for different data types (number, percentage, currency, text, boolean)
- Conditional display based on product attributes
- Audit trail for all changes

### 4. Milestone Management
- Create and manage strategic milestones
- RAG status tracking (Red, Amber, Green)
- Progress updates with risk and issue tracking
- Category classification (strategic, flagship, mission, etc.)

### 5. Automated Reminders
- Due reports (1st of following month)
- Late reports (by 5th working day)
- Overdue reports (10+ working days)
- GOV.UK Notify integration for email notifications

## Technical Implementation

### Database Schema

The feature uses SQLite locally and Azure SQL Database in production with the following entities:

- **ProductContact**: Links users to products they're responsible for
- **PerformanceMetric**: Configurable metrics from CMS
- **Milestone**: Strategic milestones and project deliverables
- **MilestoneUpdate**: Progress updates and notes
- **PerformanceMetricValue**: Actual reported values
- **ReportingPeriod**: Reporting periods and status
- **ReportingAuditLog**: Complete audit trail
- **ReportingReminder**: Reminder tracking

### CMS Content Types

New Strapi content types created:
- `performance-metric`: Configurable performance metrics
- `product-contact`: User-product relationships
- `reporting-period`: Reporting periods
- `milestone`: Strategic milestones
- `milestone-update`: Milestone progress updates

### Services

- **ReportingService**: Core business logic and data operations
- **NotificationService**: GOV.UK Notify integration for reminders

### Controllers

- **ReportingController**: Handles all reporting-related requests
- Routes: `/reports`, `/reports/services`, `/reports/services/{id}`, etc.

## Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "ReportingDb": "Data Source=reporting.db"
  }
}
```

### GOV.UK Notify
```json
{
  "GOVUKNotify": {
    "ApiKey": "YOUR_GOVUK_NOTIFY_API_KEY",
    "Templates": {
      "DueReport": "YOUR_DUE_REPORT_TEMPLATE_ID",
      "LateReport": "YOUR_LATE_REPORT_TEMPLATE_ID", 
      "OverdueReport": "YOUR_OVERDUE_REPORT_TEMPLATE_ID",
      "ReportSubmitted": "YOUR_REPORT_SUBMITTED_TEMPLATE_ID"
    }
  }
}
```

## Usage

### For Product Contacts

1. **Access Dashboard**: Navigate to `/reports` to see all your assigned services
2. **Submit Reports**: Click on a service to access the reporting form
3. **Update Metrics**: Complete performance metrics by category
4. **Manage Milestones**: Create and update strategic milestones
5. **Submit Report**: Submit completed reports for review

### For Administrators

1. **Configure Metrics**: Use CMS to create and manage performance metrics
2. **Assign Contacts**: Link users to products they're responsible for
3. **Monitor Progress**: Track completion rates and overdue reports
4. **Review Submissions**: Access submitted reports for analysis

## Design Patterns

The implementation follows the DfE Register of Services design patterns:

- **GOV.UK Design System**: Consistent styling and components
- **Tabbed Navigation**: Organised content sections
- **Summary Cards**: Clear information display
- **Status Indicators**: Visual progress tracking
- **Accessible Forms**: WCAG compliant input controls

## Security Features

- **User Authorization**: Only assigned product contacts can access reports
- **Audit Logging**: Complete trail of all changes
- **Data Validation**: Server-side validation for all inputs
- **CSRF Protection**: Anti-forgery tokens on all forms

## Future Enhancements

- **Data Export**: CSV/Excel export functionality
- **Dashboard Analytics**: Visual charts and trends
- **Integration APIs**: Connect with external monitoring tools
- **Advanced Reporting**: Custom report builder
- **Workflow Management**: Approval processes and notifications

## Testing

Use the `TestReportingData.cs` script to populate sample data for testing:

```bash
dotnet run TestReportingData.cs
```

This creates sample product contacts, reporting periods, milestones, and performance metric values for testing the functionality.

## Deployment

### Local Development
1. Ensure SQLite database is created on first run
2. Configure GOV.UK Notify settings (optional for development)
3. Run the test data script to populate sample data

### Production
1. Configure Azure SQL Database connection string
2. Set up GOV.UK Notify API key and template IDs
3. Deploy CMS content types
4. Configure product contacts in CMS

## Support

For questions or issues with the reporting feature, contact the development team or create an issue in the project repository.
