# Frontend Admin Interface

This document describes the admin functionality for managing product information in the FIPS frontend application.

## Overview

The admin interface provides a comprehensive product management system that allows authenticated users with admin permissions to:

- View all products in the system
- Create new products
- Edit existing products
- Delete products
- Publish/unpublish products
- Manage product states (New, Active, Rejected, Removed)
- Keep products as drafts (publishedAt = null)

## Authentication & Authorization

### User Authentication
- Users must be signed in through Azure AD/Entra ID
- Authentication is handled by the existing `SecurityService`
- User information is retrieved from the current session

### Permission Checking
The system checks user permissions by:
1. Verifying the user is authenticated
2. Checking if the user has "Admin" role using `User.IsInRole("Admin")`
3. Using the `SecurityService.CanAccessResource(HttpContext, "admin")` method
4. Only users with admin permissions can access admin functionality

### API Integration
- All admin operations use the `CmsApiService` with the write API key (`CmsApi:WriteApiKey`)
- This ensures proper authorization for create, update, and delete operations
- Products are created as drafts by default (publishedAt = null) per user preference

## Admin Interface Components

### Admin Controller
Location: `Controllers/AdminController.cs`

Features:
- **Products Management**: Full CRUD operations for products
- **Authentication Checks**: Verifies admin permissions for all actions
- **Error Handling**: Comprehensive error handling with user feedback
- **Draft Management**: Creates products as drafts by default
- **Publish/Unpublish**: Separate actions for publication management

### Admin Views
Location: `Views/Admin/`

Views:
- **Index.cshtml**: Admin dashboard with navigation to different admin functions
- **Products.cshtml**: Product list with management actions
- **CreateProduct.cshtml**: Form for creating new products
- **EditProduct.cshtml**: Form for editing existing products
- **_Layout.cshtml**: Admin-specific layout with breadcrumbs

### View Models
Location: `Models/AdminModels.cs`

Models:
- **AdminProductsViewModel**: View model for the products list page
- **ProductFormViewModel**: Form model for creating/editing products with validation

## User Interface Features

### Product Management
- **Product List**: Table view with all products and their status
- **State Management**: Visual badges showing product states
- **Draft Status**: Clear indicators for published vs draft products
- **Bulk Actions**: Edit, publish, unpublish, and delete actions
- **Confirmation Dialogs**: Safety confirmations for destructive actions

### Form Features
- **Validation**: Client and server-side validation
- **Required Fields**: Title and short description are required
- **Optional Fields**: FIPS ID, CMDB System ID, long description, product URL
- **State Selection**: Dropdown for product state management
- **Draft Creation**: All products created as drafts by default

### Navigation
- **Admin Menu**: Admin link in main navigation (only visible to admin users)
- **Breadcrumbs**: Clear navigation path in admin interface
- **Phase Banner**: Admin interface identification
- **Responsive Design**: Works across different screen sizes

## API Integration

### CMS API Service
The admin interface uses the existing `CmsApiService` which:
- Uses the write API key for all modifications
- Handles authentication and error responses
- Provides caching for read operations
- Implements retry policies for reliability

### Product Operations
- **GET /api/products**: Retrieve all products with population
- **POST /api/products**: Create new product (as draft)
- **PUT /api/products/{id}**: Update existing product
- **DELETE /api/products/{id}**: Delete product
- **Publish/Unpublish**: Update publishedAt field

## Security Considerations

- All admin operations require authentication and admin role
- CSRF protection with anti-forgery tokens
- Input validation and sanitization
- Secure error handling without information disclosure
- Audit logging through existing logging infrastructure

## Usage

### Accessing the Admin Interface
1. Sign in to the application with an admin account
2. Navigate to the "Admin" link in the main navigation
3. Use the admin dashboard to access different functions
4. Manage products through the product management interface

### Creating Products
1. Click "Add New Product" from the products page
2. Fill in required fields (title, short description)
3. Optionally fill in other fields
4. Select product state
5. Click "Create Product" (product will be saved as draft)

### Editing Products
1. Click "Edit" next to any product in the list
2. Modify the desired fields
3. Click "Update Product"

### Publishing Products
1. From the product list, click "Publish" next to a draft product
2. Confirm the publication
3. Product will be published with current timestamp

### Deleting Products
1. Click "Delete" next to any product
2. Confirm the deletion in the dialog
3. Product will be permanently removed

## Development Notes

- The admin interface uses GOV.UK Frontend design system components
- All operations respect the user preference for keeping products as drafts
- The interface includes proper loading states and error handling
- Responsive design ensures usability across different screen sizes
- Integration with existing authentication and security infrastructure
