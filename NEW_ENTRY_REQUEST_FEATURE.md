# New Product Entry Request Feature

## Overview
This feature allows users to request that a new product be added to FIPS. The form is similar to the "Propose Change" feature but is for brand new products rather than changes to existing ones.

## Files Created/Modified

### New Files
1. **Models/RequestNewEntryViewModel.cs** - View model for the new entry request form
2. **Views/Product/request-new-entry.cshtml** - The form view for requesting new entries
3. **NEW_ENTRY_REQUEST_FEATURE.md** - This documentation file

### Modified Files
1. **Services/NotifyService.cs** - Added `SendNewEntryRequestEmailAsync` method to INotifyService interface and implementation
2. **Controllers/ProductsController.cs** - Added `RequestNewEntry` GET and POST actions plus `ReloadRequestNewEntryForm` helper method
3. **Views/Products/Index.cshtml** - Added navigation link to the new entry request form

## URL Route
The new entry request form is accessible at:
```
/products/requestnewentry
```

## Navigation
A link to the new entry request form has been added to the products listing page (`/products`) in the inset text box at the top of the results area. The text reads:
> "Can't find a product or service? **Request a new entry** or find out how to add one."

## Form Fields

### Required Fields
- **Title** - Product title (max 255 characters)
- **Description** - Product description (max 2000 characters)
- **Notes** - Additional information about the request (max 2000 characters)

### Optional Fields
- **Service URL** - Web address of the product (max 500 characters)
- **Phase** - Radio buttons from CMS Phase category values
- **Business Area** - Radio buttons from CMS Business Area category values
- **Channels** - Checkboxes from CMS Channel category values
- **Types** - Checkboxes from CMS Type category values
- **Users** - Description of who uses the product (max 2000 characters)
- **Delivery Manager** - Name (max 255 characters)
- **Product Manager** - Name (max 255 characters)
- **Senior Responsible Officer** - Name (max 255 characters)

## Email Notification

When a new entry request is submitted, an email is sent via GOV.UK Notify to the FIPS mailbox with the following parameters:

- `requestor` - Name and email of the person submitting the request
- `title` - Product title
- `description` - Product description
- `phase` - Selected phase (or "(not specified)")
- `businessArea` - Selected business area (or "(not specified)")
- `channels` - Selected channels as comma-separated list (or "(not specified)")
- `type` - Selected types as comma-separated list (or "(not specified)")
- `serviceURL` - Service URL (or "(not specified)")
- `users` - User description (or "(not specified)")
- `deliveryManager` - Delivery Manager name (or "(not specified)")
- `productManager` - Product Manager name (or "(not specified)")
- `sro` - Senior Responsible Officer name (or "(not specified)")
- `notes` - Additional notes

## Configuration

The feature uses the following configuration in `appsettings.json`:

```json
"Notify": {
  "ApiKey": "your-notify-api-key",
  "Templates": {
    "changeEntry": "existing-template-id",
    "newEntry": "1bf0865f-49d5-4bef-983a-9fc62ad2ed93"
  },
  "FIPSMailbox": "FIPS.SERVICE@education.gov.uk"
}
```

The `newEntry` template ID should be configured in GOV.UK Notify with the personalisation fields listed above.

## Feature Flag

This feature uses the same feature flag as the "Propose Change" feature:
```json
"EnabledFeatures": {
  "EditProduct": true
}
```

## User Experience

1. User navigates to `/products/requestnewentry`
2. User fills in the form with product details
3. User submits the form
4. An email is sent to the FIPS mailbox with all the details
5. A success message is displayed to the user
6. The form is cleared, ready for another submission if needed

## Access Control

- The feature respects the `EditProduct` feature flag
- Users must be permanent DfE staff (indicated by inset text warning)
- User authentication claims are used to identify the requestor

## Error Handling

- Form validation ensures required fields are completed
- Character limits are enforced on all text fields
- If email sending fails, an error message is displayed to the user
- Detailed logging is provided for debugging

## GOV.UK Design System Compliance

[[memory:6984069]]
The form follows GOV.UK Design System patterns:
- Uses standard form components (inputs, textareas, radios, checkboxes)
- Includes proper error summaries and field-level error messages
- Uses appropriate ARIA attributes for accessibility
- Includes visually hidden text for screen readers
- Follows GOV.UK Frontend styling conventions
- Supports keyboard navigation (tab order)
- Uses `rel="noopener noreferrer"` on links [[memory:3426845]]
- HTML tags are written on single lines [[memory:2963825]]
- Uses British English spelling [[memory:2963817]]

## Testing

To test the feature:
1. Ensure the `EditProduct` feature flag is enabled
2. Navigate to `/products/requestnewentry`
3. Fill in the required fields (Title, Description, Notes)
4. Optionally fill in other fields
5. Submit the form
6. Verify the email is received at the FIPS mailbox
7. Verify the success message is displayed

