# Maintenance Mode Feature

This document explains how the maintenance mode feature works in the FIPS frontend application.

## Overview

The maintenance mode feature automatically detects when the CMS (Content Management System) is unavailable and displays a user-friendly maintenance page instead of showing errors or broken content.

## How It Works

### Automatic Detection
- The system continuously monitors the CMS health by making periodic health checks
- If the CMS becomes unavailable, users are automatically redirected to a maintenance page
- The health check is cached for 30 seconds to avoid excessive API calls

### Manual Override
- Administrators can manually enable maintenance mode through configuration
- This is useful for planned maintenance windows

## Configuration

### appsettings.json
```json
{
  "MaintenanceMode": {
    "Enabled": false,
    "HealthCheckIntervalSeconds": 30,
    "HealthCheckTimeoutSeconds": 10
  }
}
```

### Settings Explained
- `Enabled`: Set to `true` to manually enable maintenance mode
- `HealthCheckIntervalSeconds`: How often to check CMS health (default: 30 seconds)
- `HealthCheckTimeoutSeconds`: Timeout for health check requests (default: 10 seconds)

## Files Added/Modified

### New Files
- `Services/CmsHealthService.cs` - Service for monitoring CMS health
- `Middlewares/MaintenanceMiddleware.cs` - Middleware to intercept requests during maintenance
- `Controllers/MaintenanceController.cs` - Controller for maintenance page and health API
- `Views/Maintenance/Index.cshtml` - Maintenance page view

### Modified Files
- `Program.cs` - Added service registration and middleware pipeline
- `appsettings.json` - Added maintenance mode configuration

## Usage

### Automatic Mode
The system automatically detects CMS unavailability and shows the maintenance page. No manual intervention required.

### Manual Mode
To manually enable maintenance mode:

1. Set `MaintenanceMode.Enabled` to `true` in `appsettings.json`
2. Restart the application
3. All users will see the maintenance page

### Health Check API
The system provides a health check endpoint at `/api/health` that returns:
```json
{
  "status": "healthy|unhealthy",
  "cmsAvailable": true|false,
  "maintenanceMode": true|false,
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Excluded Paths

The following paths are excluded from maintenance mode and will continue to work:
- `/health`
- `/api/health`
- `/maintenance`
- `/css`
- `/js`
- `/images`
- `/favicon.ico`
- `/robots.txt`

## Maintenance Page Features

The maintenance page includes:
- Clear explanation of the situation
- User-friendly messaging
- "Try Again" button to refresh
- Contact support link
- Technical information section for administrators
- Real-time health status check

## Testing

To test the maintenance mode:

1. **Automatic Mode**: Stop the CMS service and visit any page
2. **Manual Mode**: Set `MaintenanceMode.Enabled` to `true` and restart the app
3. **Health Check**: Visit `/api/health` to see current system status

## Troubleshooting

If maintenance mode is not working as expected:

1. Check the application logs for health check errors
2. Verify the CMS API configuration in `appsettings.json`
3. Ensure the maintenance middleware is registered in `Program.cs`
4. Check that the maintenance controller and view files exist
