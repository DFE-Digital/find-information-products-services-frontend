# FIPS Azure Release Plan

## Overview

This document provides a comprehensive step-by-step guide for deploying the FIPS CMS (Strapi) and Frontend (ASP.NET Core) applications to Azure with PostgreSQL database, including Entra ID authentication setup and GitHub Actions CI/CD configuration.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Azure Infrastructure Setup](#azure-infrastructure-setup)
3. [Entra ID App Registrations](#entra-id-app-registrations)
4. [PostgreSQL Database Setup](#postgresql-database-setup)
5. [CMS Deployment](#cms-deployment)
6. [Frontend Deployment](#frontend-deployment)
7. [GitHub Actions Configuration](#github-actions-configuration)
8. [Security Configuration](#security-configuration)
9. [Monitoring and Logging](#monitoring-and-logging)
10. [Testing and Validation](#testing-and-validation)
11. [Maintenance Procedures](#maintenance-procedures)

## Prerequisites

### Azure Requirements
- **Azure Subscription**: Active subscription with appropriate permissions
- **Resource Group**: Dedicated resource group for FIPS resources
- **Azure CLI**: Version 2.50.0 or later
- **PowerShell**: Version 7.0 or later (for Azure PowerShell modules)

### Development Tools
- **Git**: Version 2.30 or later
- **Node.js**: Version 20.x (for CMS)
- **.NET SDK**: Version 8.0.x (for Frontend)
- **Azure Functions Core Tools**: Version 4.x (for sync-app)

### Team Access
- **Azure Administrator**: For resource creation and permissions
- **Entra ID Administrator**: For app registrations and permissions
- **Database Administrator**: For PostgreSQL setup and configuration
- **DevOps Engineer**: For CI/CD pipeline configuration

## Azure Infrastructure Setup

### 1. Resource Group Creation

1. **Navigate to Azure Portal**
   - Go to [portal.azure.com](https://portal.azure.com)
   - Sign in with your Azure account

2. **Create Resource Group**
   - Click "Create a resource" → "Resource group"
   - **Resource group name**: `rg-fips-production`
   - **Region**: UK South
   - **Tags**: 
     - Environment: Production
     - Project: FIPS
     - Owner: DigitalOps
   - Click "Review + create" → "Create"

### 2. App Service Plans

#### CMS App Service Plan
1. **Create App Service Plan**
   - Click "Create a resource" → "App Service Plan"
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Name**: `asp-fips-cms`
   - **Operating System**: Linux
   - **Region**: UK South
   - **Pricing tier**: B2 (Basic)
   - Click "Review + create" → "Create"

#### Frontend App Service Plan
1. **Create App Service Plan**
   - Click "Create a resource" → "App Service Plan"
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Name**: `asp-fips-frontend`
   - **Operating System**: Windows
   - **Region**: UK South
   - **Pricing tier**: B2 (Basic)
   - Click "Review + create" → "Create"

### 3. Storage Account

1. **Create Storage Account**
   - Click "Create a resource" → "Storage account"
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Storage account name**: `stfipsprod[random-number]` (must be globally unique)
   - **Region**: UK South
   - **Performance**: Standard
   - **Redundancy**: Locally-redundant storage (LRS)
   - Click "Review + create" → "Create"

### 4. Application Insights

1. **Create Application Insights**
   - Click "Create a resource" → "Application Insights"
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Name**: `ai-fips-production`
   - **Region**: UK South
   - **Resource Mode**: Classic
   - Click "Review + create" → "Create"

## Entra ID App Registrations

### 1. CMS App Registration

#### Create App Registration
1. **Navigate to Entra ID**
   - Go to [portal.azure.com](https://portal.azure.com)
   - Navigate to "Azure Active Directory" → "App registrations"

2. **Create New Registration**
   - Click "New registration"
   - **Name**: `FIPS CMS`
   - **Supported account types**: Accounts in this organisational directory only
   - **Redirect URI**: Web - `https://fips-cms-prod.azurewebsites.net/admin/auth/callback`
   - Click "Register"

3. **Note Important Values**
   - **Application (client) ID**: Copy this value
   - **Directory (tenant) ID**: Copy this value

#### Configure API Permissions
1. **Add Microsoft Graph Permissions**
   - In your app registration, go to "API permissions"
   - Click "Add a permission"
   - Select "Microsoft Graph"
   - Choose "Delegated permissions"
   - Select "User.Read"
   - Click "Add permissions"

2. **Grant Admin Consent**
   - Click "Grant admin consent for [Your Organisation]"
   - Confirm the action

#### Create Client Secret
1. **Generate Secret**
   - Go to "Certificates & secrets"
   - Click "New client secret"
   - **Description**: `FIPS CMS Production Secret`
   - **Expires**: 24 months
   - Click "Add"
   - **Important**: Copy the secret value immediately (it won't be shown again)

### 2. Frontend App Registration

#### Create App Registration
1. **Create New Registration**
   - In Entra ID → "App registrations"
   - Click "New registration"
   - **Name**: `FIPS Frontend`
   - **Supported account types**: Accounts in this organisational directory only
   - **Redirect URI**: Web - `https://fips-frontend-prod.azurewebsites.net/signin-oidc`
   - Click "Register"

2. **Note Important Values**
   - **Application (client) ID**: Copy this value
   - **Directory (tenant) ID**: Copy this value

#### Configure API Permissions
1. **Add Microsoft Graph Permissions**
   - In your app registration, go to "API permissions"
   - Click "Add a permission"
   - Select "Microsoft Graph"
   - Choose "Delegated permissions"
   - Select "User.Read.All"
   - Click "Add permissions"

2. **Grant Admin Consent**
   - Click "Grant admin consent for [Your Organisation]"
   - Confirm the action

#### Create Client Secret
1. **Generate Secret**
   - Go to "Certificates & secrets"
   - Click "New client secret"
   - **Description**: `FIPS Frontend Production Secret`
   - **Expires**: 24 months
   - Click "Add"
   - **Important**: Copy the secret value immediately (it won't be shown again)

### 3. Service Principal Creation

Service principals are automatically created when you register applications in Entra ID. No additional steps required.

## PostgreSQL Database Setup

### 1. Create PostgreSQL Server

1. **Navigate to Azure Database for PostgreSQL**
   - Go to [portal.azure.com](https://portal.azure.com)
   - Click "Create a resource" → "Azure Database for PostgreSQL"

2. **Select Flexible Server**
   - Click "Create" under "Flexible server"
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Server name**: `psql-fips-prod`
   - **Region**: UK South
   - **PostgreSQL version**: 15
   - **Workload type**: Development
   - **Compute + storage**: Burstable, B2s (2 vCores, 4 GB RAM)
   - **Storage size**: 32 GB
   - **Backup retention**: 7 days
   - Click "Next: Networking"

3. **Configure Networking**
   - **Connectivity method**: Public access (selected IP addresses)
   - **Firewall rules**: Add current client IP address
   - **Allow access to Azure services**: Yes
   - Click "Next: Security"

4. **Configure Security**
   - **Admin username**: `fipsadmin`
   - **Password**: Create a strong password (save this securely)
   - **Confirm password**: Re-enter the password
   - Click "Review + create" → "Create"

### 2. Create Database

1. **Navigate to Your PostgreSQL Server**
   - Go to your PostgreSQL server in the Azure Portal
   - Click "Databases" in the left menu

2. **Create New Database**
   - Click "Add database"
   - **Database name**: `fips_cms`
   - **Collation**: `en_US.UTF8`
   - **Character set**: `UTF8`
   - Click "OK"

### 3. Configure Firewall Rules

1. **Access Firewall Settings**
   - In your PostgreSQL server, go to "Networking"
   - Click "Add current client IP address" to add your current IP
   - Click "Add 0.0.0.0 - 255.255.255.255" to allow Azure services
   - Click "Save"

### 4. Enable SSL

1. **Configure SSL Settings**
   - In your PostgreSQL server, go to "Server parameters"
   - Search for "ssl_enforcement"
   - Set value to "ON"
   - Click "Save"

## CMS Deployment

### 1. Create Web App

1. **Navigate to App Services**
   - Go to [portal.azure.com](https://portal.azure.com)
   - Click "Create a resource" → "Web App"

2. **Configure Basic Settings**
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Name**: `fips-cms-prod`
   - **Publish**: Code
   - **Runtime stack**: Node 20 LTS
   - **Operating System**: Linux
   - **Region**: UK South
   - **App Service Plan**: `asp-fips-cms`
   - Click "Review + create" → "Create"

### 2. Configure Application Settings

1. **Access Configuration**
   - Navigate to your Web App
   - Go to "Settings" → "Configuration"

2. **Add Application Settings**
   Click "New application setting" for each of the following:

   **Database Configuration:**
   - `DATABASE_CLIENT` = `postgres`
   - `DATABASE_HOST` = `psql-fips-prod.postgres.database.azure.com`
   - `DATABASE_PORT` = `5432`
   - `DATABASE_NAME` = `fips_cms`
   - `DATABASE_USERNAME` = `fipsadmin`
   - `DATABASE_PASSWORD` = `[Your PostgreSQL Password]`
   - `DATABASE_SSL` = `true`
   - `DATABASE_SCHEMA` = `public`

   **Strapi Configuration:**
   - `NODE_ENV` = `production`
   - `HOST` = `0.0.0.0`
   - `PORT` = `1337`
   - `APP_KEYS` = `[Generate two random 32-character strings]`
   - `API_TOKEN_SALT` = `[Generate random 32-character string]`
   - `ADMIN_JWT_SECRET` = `[Generate random 32-character string]`
   - `TRANSFER_TOKEN_SALT` = `[Generate random 32-character string]`
   - `JWT_SECRET` = `[Generate random 32-character string]`
   - `PUBLIC_URL` = `https://fips-cms-prod.azurewebsites.net`

   **Entra ID Configuration:**
   - `AZURE_AD_CLIENT_ID` = `[Your CMS App Registration Client ID]`
   - `AZURE_AD_CLIENT_SECRET` = `[Your CMS Client Secret]`
   - `AZURE_AD_TENANT_ID` = `[Your Tenant ID]`
   - `AZURE_AD_REDIRECT_URI` = `https://fips-cms-prod.azurewebsites.net/admin/auth/callback`

3. **Save Configuration**
   - Click "Save" to apply all settings

### 3. Configure Startup Command

1. **Access General Settings**
   - Go to "Settings" → "General settings"

2. **Set Startup Command**
   - **Startup Command**: `npm start`
   - Click "Save"

## Frontend Deployment

### 1. Create Web App

1. **Navigate to App Services**
   - Go to [portal.azure.com](https://portal.azure.com)
   - Click "Create a resource" → "Web App"

2. **Configure Basic Settings**
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Name**: `fips-frontend-prod`
   - **Publish**: Code
   - **Runtime stack**: .NET 8
   - **Operating System**: Windows
   - **Region**: UK South
   - **App Service Plan**: `asp-fips-frontend`
   - Click "Review + create" → "Create"

### 2. Configure Application Settings

1. **Access Configuration**
   - Navigate to your Web App
   - Go to "Settings" → "Configuration"

2. **Add Application Settings**
   Click "New application setting" for each of the following:

   **Azure AD Configuration:**
   - `AzureAd__Instance` = `https://login.microsoftonline.com/`
   - `AzureAd__Domain` = `[Your Domain].onmicrosoft.com`
   - `AzureAd__TenantId` = `[Your Tenant ID]`
   - `AzureAd__ClientId` = `[Your Frontend App Registration Client ID]`
   - `AzureAd__ClientSecret` = `[Your Frontend Client Secret]`
   - `AzureAd__SecretId` = `[Your Secret ID]`
   - `AzureAd__CallbackPath` = `/signin-oidc`

   **CMS API Configuration:**
   - `CmsApi__BaseUrl` = `https://fips-cms-prod.azurewebsites.net/api`
   - `CmsApi__ReadApiKey` = `[Your CMS Read API Key]`
   - `CmsApi__WriteApiKey` = `[Your CMS Write API Key]`

   **Application Configuration:**
   - `BaseUrl` = `https://fips-frontend-prod.azurewebsites.net`
   - `ConnectionStrings__ReportingDb` = `Data Source=reporting.db`
   - `MicrosoftGraph__Scopes` = `User.Read.All`

   **GOV.UK Notify Configuration:**
   - `Notify__ApiKey` = `[Your GOV.UK Notify API Key]`
   - `Notify__Templates__changeEntry` = `[Your Template ID]`
   - `Notify__FIPSMailbox` = `FIPS.SERVICE@education.gov.uk`

   **Application Insights:**
   - `ApplicationInsights__InstrumentationKey` = `[Your App Insights Key]`
   - `ApplicationInsights__ConnectionString` = `[Your App Insights Connection String]`

3. **Save Configuration**
   - Click "Save" to apply all settings

## GitHub Actions Configuration

### 1. Update Existing Workflows for Production

You already have GitHub Actions workflows configured! Here's how to update them for production deployment:

#### CMS Workflow (`cms/.github/workflows/azure-webapps-node.yml`)
Update the following values in your existing workflow:

1. **Update Environment Variables:**
   - Change `AZURE_WEBAPP_NAME` from `fips-cms` to `fips-cms-prod`

2. **Update Environment:**
   - Change environment name from `'Development'` to `'Production'`

3. **Update Secret Name:**
   - Change `AZURE_WEBAPP_PUBLISH_PROFILE` to `AZURE_WEBAPP_PUBLISH_PROFILE_CMS_PROD`

#### Frontend Workflow (`frontend/.github/workflows/azure-webapps-dotnet-core.yml`)
Update the following values in your existing workflow:

1. **Update Environment Variables:**
   - Change `AZURE_WEBAPP_NAME` from `fips-frontend` to `fips-frontend-prod`

2. **Update Environment:**
   - Change environment name from `'Development'` to `'Production'`

3. **Update Secret Name:**
   - Change `AZURE_WEBAPP_PUBLISH_PROFILE` to `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND_PROD`

### 2. GitHub Secrets Configuration

Configure the following secrets in GitHub repository settings:

1. **Navigate to GitHub Repository Settings**
   - Go to your repository on GitHub
   - Click "Settings" → "Secrets and variables" → "Actions"

2. **Add Production Secrets**
   Click "New repository secret" for each:

   **CMS Production Publish Profile:**
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE_CMS_PROD`
   - **Secret**: [Paste CMS production publish profile content]

   **Frontend Production Publish Profile:**
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND_PROD`
   - **Secret**: [Paste Frontend production publish profile content]

### 3. Obtain Publish Profiles

To get the publish profiles for your production Web Apps:

1. **CMS Publish Profile:**
   - Go to Azure Portal → App Services → `fips-cms-prod` → Overview
   - Click "Get publish profile"
   - Copy the entire XML content

2. **Frontend Publish Profile:**
   - Go to Azure Portal → App Services → `fips-frontend-prod` → Overview
   - Click "Get publish profile"
   - Copy the entire XML content

### 4. Environment Protection Rules (Optional)

For additional security, configure environment protection rules:

1. **Navigate to Environments**
   - Go to GitHub repository → Settings → Environments
   - Click "New environment"
   - **Name**: `Production`

2. **Configure Protection Rules**
   - **Required reviewers**: Add team members who must approve deployments
   - **Wait timer**: Set to 0 minutes (or desired delay)
   - **Deployment branches**: Restrict to `main` branch only

## Security Configuration

### 1. Azure Key Vault Setup

1. **Create Key Vault**
   - Go to [portal.azure.com](https://portal.azure.com)
   - Click "Create a resource" → "Key Vault"
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Key vault name**: `kv-fips-prod`
   - **Region**: UK South
   - **Pricing tier**: Standard
   - Click "Review + create" → "Create"

### 2. Store Secrets in Key Vault

1. **Access Key Vault**
   - Navigate to your Key Vault
   - Go to "Secrets" in the left menu

2. **Add Secrets**
   Click "Generate/Import" for each secret:

   **Database Password:**
   - **Upload options**: Manual
   - **Name**: `database-password`
   - **Value**: `[Your PostgreSQL Password]`
   - Click "Create"

   **CMS Client Secret:**
   - **Upload options**: Manual
   - **Name**: `cms-client-secret`
   - **Value**: `[Your CMS Client Secret]`
   - Click "Create"

   **Frontend Client Secret:**
   - **Upload options**: Manual
   - **Name**: `frontend-client-secret`
   - **Value**: `[Your Frontend Client Secret]`
   - Click "Create"

   **GOV.UK Notify API Key:**
   - **Upload options**: Manual
   - **Name**: `govuk-notify-api-key`
   - **Value**: `[Your GOV.UK Notify API Key]`
   - Click "Create"

### 3. Configure Managed Identity

1. **Enable Managed Identity for CMS**
   - Navigate to your CMS Web App
   - Go to "Settings" → "Identity"
   - **System assigned**: Status = On
   - Click "Save"

2. **Enable Managed Identity for Frontend**
   - Navigate to your Frontend Web App
   - Go to "Settings" → "Identity"
   - **System assigned**: Status = On
   - Click "Save"

3. **Grant Key Vault Access**
   - Navigate to your Key Vault
   - Go to "Access policies"
   - Click "Create"

   **For CMS:**
   - **Configure from template**: Key, Secret, Certificate Management
   - **Principal**: Search for your CMS Web App name
   - **Secret permissions**: Get, List
   - Click "Next" → "Create"

   **For Frontend:**
   - **Configure from template**: Key, Secret, Certificate Management
   - **Principal**: Search for your Frontend Web App name
   - **Secret permissions**: Get, List
   - Click "Next" → "Create"

### 4. Update Application Settings to Use Key Vault

1. **Update CMS Settings**
   - Navigate to your CMS Web App
   - Go to "Settings" → "Configuration"
   - Update the following settings:
     - `DATABASE_PASSWORD` = `@Microsoft.KeyVault(VaultName=kv-fips-prod;SecretName=database-password)`
     - `AZURE_AD_CLIENT_SECRET` = `@Microsoft.KeyVault(VaultName=kv-fips-prod;SecretName=cms-client-secret)`
   - Click "Save"

2. **Update Frontend Settings**
   - Navigate to your Frontend Web App
   - Go to "Settings" → "Configuration"
   - Update the following settings:
     - `AzureAd__ClientSecret` = `@Microsoft.KeyVault(VaultName=kv-fips-prod;SecretName=frontend-client-secret)`
     - `Notify__ApiKey` = `@Microsoft.KeyVault(VaultName=kv-fips-prod;SecretName=govuk-notify-api-key)`
   - Click "Save"

## Monitoring and Logging

### 1. Application Insights Configuration

1. **Get Connection String**
   - Navigate to your Application Insights resource
   - Go to "Overview"
   - Copy the "Connection String" value

2. **Configure Web Apps**
   - Navigate to each Web App
   - Go to "Settings" → "Configuration"
   - Add/Update:
     - `APPINSIGHTS_INSTRUMENTATIONKEY` = `[Your Instrumentation Key]`
     - `APPLICATIONINSIGHTS_CONNECTION_STRING` = `[Your Connection String]`
   - Click "Save"

### 2. Configure Log Analytics

1. **Create Log Analytics Workspace**
   - Go to [portal.azure.com](https://portal.azure.com)
   - Click "Create a resource" → "Log Analytics workspace"
   - **Subscription**: Select your subscription
   - **Resource Group**: `rg-fips-production`
   - **Name**: `law-fips-prod`
   - **Region**: UK South
   - Click "Review + create" → "Create"

### 3. Set Up Alerts

1. **Create Alert Rule**
   - Go to "Monitor" → "Alerts"
   - Click "Create" → "Alert rule"

2. **Configure Alert**
   - **Scope**: Select your CMS Web App
   - **Condition**: Click "Add condition"
   - **Signal type**: Metrics
   - **Signal name**: HTTP 5xx
   - **Threshold**: Greater than 10
   - **Aggregation type**: Count
   - **Aggregation granularity**: 5 minutes
   - **Frequency of evaluation**: 5 minutes
   - Click "Done"

3. **Configure Actions**
   - **Action group**: Create new or select existing
   - **Alert rule name**: "High Error Rate - CMS"
   - **Severity**: Warning
   - Click "Create alert rule"

## Testing and Validation

### 1. Pre-Deployment Testing

```bash
# Test database connectivity
psql "host=psql-fips-prod.postgres.database.azure.com port=5432 dbname=fips_cms user=fipsadmin password=YourSecurePassword123! sslmode=require"

# Test CMS API endpoints
curl -H "Authorization: Bearer YOUR_API_TOKEN" \
  https://fips-cms-prod.azurewebsites.net/api/products

# Test Frontend authentication
curl -I https://fips-frontend-prod.azurewebsites.net
```

### 2. Post-Deployment Validation

#### CMS Validation Checklist
- [ ] Admin panel accessible at `/admin`
- [ ] API endpoints responding correctly
- [ ] Database connectivity working
- [ ] File uploads functioning
- [ ] Entra ID authentication working

#### Frontend Validation Checklist
- [ ] Homepage loading correctly
- [ ] Authentication flow working
- [ ] CMS API integration functioning
- [ ] GOV.UK Frontend styling applied
- [ ] All pages accessible

### 3. Performance Testing

```bash
# Load test CMS API
ab -n 1000 -c 10 https://fips-cms-prod.azurewebsites.net/api/products

# Load test Frontend
ab -n 1000 -c 10 https://fips-frontend-prod.azurewebsites.net
```

## Maintenance Procedures

### 1. Regular Maintenance Tasks

#### Weekly Tasks
- [ ] Review Application Insights logs
- [ ] Check database performance metrics
- [ ] Verify backup completion
- [ ] Monitor security alerts

#### Monthly Tasks
- [ ] Update dependencies
- [ ] Review and rotate secrets
- [ ] Performance optimization review
- [ ] Security patch review

#### Quarterly Tasks
- [ ] Disaster recovery testing
- [ ] Security audit
- [ ] Capacity planning review
- [ ] Cost optimization review

### 2. Backup Procedures

1. **Database Backup**
   - Navigate to your PostgreSQL server
   - Go to "Backup" in the left menu
   - Click "Configure backup"
   - **Backup retention**: 7 days
   - **Geo-redundant backup**: Enabled
   - Click "Save"

2. **Application Settings Backup**
   - Navigate to each Web App
   - Go to "Settings" → "Configuration"
   - Click "Download" to export settings as JSON
   - Save files as `cms-settings-backup.json` and `frontend-settings-backup.json`

### 3. Disaster Recovery

#### Recovery Time Objectives (RTO)
- **CMS**: 4 hours
- **Frontend**: 2 hours
- **Database**: 1 hour

#### Recovery Point Objectives (RPO)
- **CMS**: 1 hour
- **Frontend**: 15 minutes
- **Database**: 15 minutes

#### Recovery Procedures
1. **Database Recovery**
   - Navigate to your PostgreSQL server
   - Go to "Backup" → "Restore"
   - Select backup point
   - Create new server with restored data
   - Update connection strings in Web Apps

2. **Application Recovery**
   - Redeploy from GitHub Actions
   - Update connection strings to point to restored database
   - Verify application functionality

### 4. Monitoring Dashboard

1. **Create Azure Dashboard**
   - Go to "Dashboard" in Azure Portal
   - Click "New dashboard"
   - **Dashboard name**: "FIPS Production Monitoring"

2. **Add Monitoring Tiles**
   - **Application Insights**: Request count, response time, error rate
   - **Database**: Connection count, CPU usage, storage usage
   - **Web Apps**: CPU usage, memory usage, HTTP status codes
   - **Key Vault**: Secret access logs

## Troubleshooting

### Common Issues

#### 1. Database Connection Issues
**Symptoms**: CMS unable to connect to PostgreSQL
**Solutions**:
- Verify firewall rules
- Check connection string format
- Ensure SSL is properly configured
- Verify credentials in Key Vault

#### 2. Authentication Issues
**Symptoms**: Users unable to sign in
**Solutions**:
- Verify Entra ID app registration
- Check redirect URIs
- Verify client secrets
- Check tenant ID configuration

#### 3. Performance Issues
**Symptoms**: Slow response times
**Solutions**:
- Scale up App Service Plan
- Optimize database queries
- Enable caching
- Review Application Insights data

### Support Contacts

- **Azure Support**: Azure Portal → Help + Support
- **Entra ID Support**: Microsoft 365 Admin Center
- **Database Support**: Azure Database for PostgreSQL support
- **Internal Team**: Digital Operations Team

## Cost Optimization

### 1. Resource Optimization
- Use appropriate App Service Plan sizes
- Enable auto-scaling for variable workloads
- Use reserved instances for predictable workloads
- Regular review of unused resources

### 2. Monitoring Costs
1. **Access Cost Management**
   - Go to "Cost Management + Billing"
   - Navigate to "Cost analysis"
   - Filter by resource group: `rg-fips-production`
   - Review monthly costs and trends

### 3. Budget Alerts
1. **Create Budget**
   - Go to "Cost Management + Billing" → "Budgets"
   - Click "Add"
   - **Scope**: Resource group `rg-fips-production`
   - **Budget amount**: £1000
   - **Period**: Monthly
   - **Alert conditions**: 80%, 100%, 120%
   - Click "Create"

---

## Conclusion

This release plan provides a comprehensive guide for deploying the FIPS CMS and Frontend applications to Azure with PostgreSQL database. Follow each section sequentially, ensuring all prerequisites are met and security configurations are properly implemented.

For questions or issues during deployment, refer to the troubleshooting section or contact the Digital Operations team.

**Last Updated**: January 2024  
**Version**: 1.0  
**Next Review**: April 2024
