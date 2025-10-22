# Propose Change Feature - Implementation Summary

## Overview
This document describes the "Propose a Change" feature that allows users to suggest changes to product information. When a user submits proposed changes, an email notification is sent to administrators via GOV.UK Notify with a detailed summary of all proposed changes. No data is stored in the CMS - all changes are communicated via email only.

## Components Implemented

### 1. GOV.UK Notify Integration
- **Package**: `GovukNotify` v7.2.0
- **Service**: `NotifyService` (`Services/NotifyService.cs`)
- **Configuration**: 
  - API Key: `Notify:ApiKey` in `appsettings.json`
  - Template ID: `Notify:Templates:changeEntry` (87bcef86-70ad-4289-8a0d-7bfb143216d5)
  - Recipient: `Notify:FIPSMailbox` (FIPS.SERVICE@education.gov.uk)

### 2. Email Notification
When a user submits a proposed change, an email is sent with the following parameters:
- **fipsid**: The FIPS ID of the product
- **fipsName**: The product title
- **entryLink**: Full URL to the product page
- **change**: A Markdown-formatted table showing before/after values
- **requestor**: The name and email of the signed-in user making the request (e.g., "John Smith (john.smith@education.gov.uk)")

The email includes changes to:
- Product title
- Short description
- Long description
- Product URL
- Categories (Phase, Business area, Channels, Types)
- Product contacts and their roles

### 3. Controller Actions
**Location**: `Controllers/ProductsController.cs`

#### GET `/product/{fipsid}/propose-change`
- Loads the product and all category values
- Prepares the form with current values pre-populated
- Displays the propose change form

#### POST `/product/{fipsid}/propose-change`
- Validates the proposed changes
- Collects all current and proposed values
- Formats changes into a Markdown table
- Sends email notification via GOV.UK Notify to the FIPS mailbox
- Redirects to the product view page with a success message

### 4. View Template
**Location**: `Views/Product/propose-change.cshtml`

Features:
- Shows current product information in the masthead
- Displays current values as hints below each form field
- Allows users to propose changes to:
  - Title
  - Short description
  - Long description
  - Product URL
  - Phase (radio buttons)
  - Business area (radio buttons)
  - Channels (checkboxes - multiple selection)
  - Types (checkboxes - multiple selection)
  - Reason for change (required textarea)
- Uses GOV.UK Design System components throughout [[memory:5190574]]
- Fully accessible with ARIA labels and error handling [[memory:6984069]]

### 5. Models
**Location**: `Models/ProposedChangeModels.cs`

- `ProposeChangeViewModel`: View model for the propose change form
- `ProposedProductContactModel`: Model for product contact changes

### 6. Routing
**Location**: `Program.cs`

Added route:
```csharp
app.MapControllerRoute(
    name: "product-propose-change",
    pattern: "product/{fipsid}/propose-change",
    defaults: new { controller = "Products", action = "ProposeChange" });
```

### 7. Navigation
**Location**: `Views/Products/ViewProduct.cshtml`

Added "Propose a change" link to the product navigation menu.

## Usage

### For Users
1. Navigate to any product page
2. Click "Propose a change" in the side navigation
3. Fill out the form with proposed changes (fields show current values as hints)
4. Provide a reason for the changes (required)
5. Submit the form
6. An email notification is sent to FIPS.SERVICE@education.gov.uk with all the proposed changes

### For Administrators
Administrators receive an email with:
- Product FIPS ID and name
- Link to the product entry
- Requestor information (name and email of the person making the request)
- Markdown table showing all proposed changes (current value → proposed value)
- Reason for the changes

Administrators can then:
1. Review the email with all proposed changes
2. Manually apply approved changes using the product edit interface
3. Reply to the requestor via email if needed

## Error Handling
- If the email fails to send, users see an error message and can try again
- All errors are logged for troubleshooting
- Email sending is wrapped in a try-catch to provide user-friendly error messages

## Email Template Requirements

The GOV.UK Notify template (ID: `changeEntry`) should include the following placeholders:
- `((fipsid))`: The FIPS ID
- `((fipsName))`: The product name
- `((entryLink))`: Link to the product page
- `((requestor))`: The name and email of the person making the request
- `((change))`: The change table (supports Markdown formatting)
- `((notes))`: Optional - reason for the change

Example template content:
```
A change has been proposed for ((fipsName)) (((fipsid))) by ((requestor)).

View the product: ((entryLink))

Proposed changes:
((change))

Reason for changes:
((notes))
```

## Dependencies
- **Notify.Client** (GovukNotify NuGet package v7.2.0)
- GOV.UK Design System components
- CMS API (for fetching product and category data only)

## Configuration Example

```json
{
  "Notify": {
    "ApiKey": "live-77f4093d-a177-4416-beee-a94e36fcecd2-b181133c-a707-42c6-a588-d6c44430a018",
    "Templates": {
      "changeEntry": "87bcef86-70ad-4289-8a0d-7bfb143216d5"
    },
    "FIPSMailbox": "FIPS.SERVICE@education.gov.uk"
  }
}
```

## Security Considerations
- All email notifications are logged for audit purposes
- User identity is captured from claims (email and name)
- Anti-forgery tokens protect the form submission
- Email content is sanitized to prevent Markdown injection
- Long descriptions are truncated in emails (max 100 characters per field)

## Testing
To test the feature:
1. Ensure the GOV.UK Notify API key is configured in `appsettings.json`
2. Navigate to a product page
3. Click "Propose a change"
4. Fill out the form with some changes and a reason
5. Submit the form
6. Check the FIPS mailbox (FIPS.SERVICE@education.gov.uk) for the notification email
7. Verify the email contains all the proposed changes in a readable format

## Design Decisions

### Why Email-Only (No Database Storage)?
The "Propose a Change" feature uses email-only notifications rather than storing proposed changes in a database because:

1. **Simplicity**: Administrators can review changes directly in their email without logging into a separate system
2. **Flexibility**: Administrators can respond to requestors directly via email for clarification
3. **No Additional UI**: No need to build an admin review interface
4. **Audit Trail**: Email provides a natural audit trail through email archives
5. **Low Volume**: The expected volume of change requests is low enough that email management is practical

## Future Enhancements
- Add session storage for draft changes (allow users to save progress)
- Add a preview step before final submission
- Add file attachment support for supporting documentation
- Add automated email acknowledgment to the change requester
- Consider database storage if volume increases significantly

