# FIPS Frontend

A .NET Core MVC application for managing Federal Information Processing Standards (FIPS) products with Entra ID authentication and Strapi CMS integration.

## Features

- **Entra ID Authentication**: Secure staff authentication using Microsoft Entra ID
- **CMS Integration**: Full CRUD operations with Strapi CMS backend
- **Product Management**: Manage FIPS products, categories, and contacts
- **Responsive UI**: Bootstrap-based responsive design

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Access to Microsoft Entra ID tenant
- Strapi CMS instance running

## Configuration

### 1. App Settings

Update `appsettings.json` and `appsettings.Development.json` with your configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "YOUR_DOMAIN.onmicrosoft.com",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "SecretId": "YOUR_SECRET_ID",
    "CallbackPath": "/signin-oidc"
  },
  "Entra": {
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },
  "CmsApi": {
    "BaseUrl": "http://localhost:1337/api",
    "ReadApiKey": "YOUR_READ_API_KEY",
    "WriteApiKey": "YOUR_WRITE_API_KEY"
  }
}
```

### 2. CMS API Keys

You'll need to obtain API keys from your Strapi CMS instance:

1. **Read API Key**: For GET operations (viewing data)
2. **Write API Key**: For POST, PUT, DELETE operations (modifying data)

### 3. Entra ID Setup

1. Register your application in Azure Portal
2. Configure redirect URIs:
   - `https://localhost:5001/signin-oidc` (development)
   - `https://yourdomain.com/signin-oidc` (production)
3. Set up API permissions as needed
4. Generate client secret

## Running the Application

### Development

```bash
dotnet run
```

The application will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### Production

```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet FipsFrontend.dll
```

## Project Structure

```
FipsFrontend/
├── Controllers/           # MVC Controllers
│   ├── HomeController.cs
│   └── ProductsController.cs
├── Models/               # Data Models
│   └── ProductModels.cs
├── Services/             # Business Logic Services
│   └── CmsApiService.cs
├── Views/                # Razor Views
│   ├── Home/
│   ├── Products/
│   └── Shared/
├── wwwroot/              # Static Files
│   ├── css/
│   └── js/
├── Program.cs            # Application Entry Point
├── appsettings.json      # Configuration
└── FipsFrontend.csproj   # Project File
```

## API Integration

The application integrates with Strapi CMS endpoints:

- **Products**: `/api/products`
- **Categories**: `/api/category-values`
- **Category Types**: `/api/category-types`
- **Product Contacts**: `/api/product-contacts`
- **Config Roles**: `/api/config-roles`

## Authentication Flow

1. User accesses the application
2. Redirected to Entra ID for authentication
3. After successful authentication, redirected back to application
4. User can now access protected resources

## Security Considerations

- All controllers require authentication (`[Authorize]` attribute)
- API keys are stored in configuration (consider using Azure Key Vault for production)
- HTTPS is enforced in production
- Anti-forgery tokens are used for form submissions

## Troubleshooting

### Common Issues

1. **Authentication Errors**: Verify Entra ID configuration and redirect URIs
2. **CMS API Errors**: Check API keys and CMS instance availability
3. **CORS Issues**: Ensure CMS allows requests from your frontend domain

### Logs

Check application logs for detailed error information. Logs are configured in `appsettings.json`.

## Contributing

1. Follow .NET coding standards
2. Add appropriate error handling
3. Include unit tests for new features
4. Update documentation as needed

