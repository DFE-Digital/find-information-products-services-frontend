# FIPS Frontend

A .NET Core MVC application for managing Federal Information Processing Standards (FIPS) products with Entra ID authentication and Strapi CMS integration.

## Features

- **Entra ID Authentication**: Secure staff authentication using Microsoft Entra ID
- **CMS Integration**: Full CRUD operations with Strapi CMS backend
- **Product Management**: Manage FIPS products, categories, and contacts
- **Responsive UI**: Bootstrap-based responsive design

## Prerequisites

- .NET SDK, the version in `global.json`
- Node.js, the version in `.nvmrc` (the build runs the Sass compiler with it)
- Visual Studio 2022 or VS Code
- Access to Microsoft Entra ID tenant
- Strapi CMS instance running

## Configuration

### 1. App Settings

Update `appsettings.json` with your configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "",
    "TenantId": "00000000-0000-0000-0000-000000000000",
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "ClientSecret": "",
    "SecretId": "00000000-0000-0000-0000-000000000000",
    "CallbackPath": "/signin-oidc"
  },
  "CmsApi": {
    "BaseUrl": "http://localhost:1337/api",
    "ReadApiKey": "",
    "WriteApiKey": ""
  },
  "Feedback": {
    "SurveyUrl": "https://example.com/your-feedback-survey"
  },
  "Contact": {
    "Email": "your-service-mailbox@example.com"
  }
}
```

`Feedback:SurveyUrl` is the external survey linked from every page's footer and from the data page; `Contact:Email` is the mailbox offered on the contact page. Omit either and the application's built-in default is used; set it blank and that link is not shown. On a hosted app set them as the application settings `Feedback__SurveyUrl` and `Contact__Email`.

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
dotnet run --project src/FipsFrontend
```

The build installs the npm packages it needs (the Sass compiler for the stylesheets) on the first
run and whenever `package.json` changes, so a fresh clone needs only the .NET SDK named in
`global.json` and Node.js (see `.nvmrc` for the version the build is tested with).

The application will be available at the addresses in `src/FipsFrontend/Properties/launchSettings.json`:
- HTTP: `http://localhost:5505`
- HTTPS: `https://localhost:7601`

### Tests

```bash
dotnet test
```

### Production

```bash
dotnet publish src/FipsFrontend -c Release -o ./publish
cd publish
dotnet FipsFrontend.dll
```

## Project Structure

```
.
├── .github/                  # Workflows and Dependabot configuration
├── docs/                     # Design notes and plans
├── src/
│   └── FipsFrontend/         # The web application
│       ├── Controllers/
│       ├── Models/
│       ├── Services/
│       ├── Views/
│       ├── wwwroot/
│       ├── Program.cs
│       ├── appsettings.template.json
│       └── FipsFrontend.csproj
├── tests/
│   └── FipsFrontend.Tests/   # Scenarios run against the application
├── Directory.Packages.props  # Every package version, in one place
├── FipsFrontend.slnx         # Solution: build or test everything from the root
├── global.json               # .NET SDK version
└── .nvmrc                    # Node.js version
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
