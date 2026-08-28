# Add "Not categorised" filter option for Business area, Channel, Phase, and Type filters

## Description

Add a "Not categorised" filter option to the Products page filters for Business area, Channel, Phase, and Type. This allows users to find products that don't have any category value assigned for these specific category types.

## Problem Statement

Users need to identify products that haven't been categorised for specific category types (Business area, Channel, Phase, Type) to ensure complete product cataloging and data quality.

## Solution

Added a "Not categorised" checkbox option at the bottom of each filter section (Business area, Channel, Phase, Type) with an "or" divider separating it from the regular category options.

## Technical Implementation

### Frontend Changes (`Views/Products/Index.cshtml`)
- Added "or" divider (`govuk-checkboxes__divider`) before the "Not categorised" option
- Added "Not categorised" checkbox for each filter type:
  - Business area (`group-not-categorised`)
  - Channel (`channel-not-categorised`)
  - Phase (`phase-not-categorised`)
  - Type (`type-not-categorised`)
- Uses value `__not_categorised__` for server-side identification
- Maintains consistent styling with existing filter options

### Backend Changes (`Controllers/ProductsController.cs`)
1. **Filter Detection**: Detects when `__not_categorised__` is selected for any filter type
2. **Server-side Filtering**: Excludes `__not_categorised__` from server-side API filters to fetch all relevant products
3. **Client-side Filtering**: Applies OR logic to filter products that either:
   - Match regular category filter values, OR
   - Have no category value for the selected category type (when "Not categorised" is selected)
4. **Display Logic**: Shows "Not categorised" (instead of `__not_categorised__`) in the selected filters display
5. **Pagination**: When "not categorised" filters are active, fetches all products first, applies filtering, then paginates client-side

### Key Implementation Details
- When "Not categorised" is selected alone: Shows products with no category value for that type
- When combined with regular filters: Shows products that match regular filters OR have no category value (OR logic)
- Ensures full product details (title, description) are included by using `GetProductsForListingAsync2` instead of `GetProductsForFilterCountsAsync`
- Handles pagination correctly after client-side filtering

## Process

1. ✅ Added "Not categorised" UI elements to all four filter sections in the view
2. ✅ Updated controller to detect `__not_categorised__` filter values
3. ✅ Implemented server-side filter exclusion for "not categorised" category types
4. ✅ Added client-side filtering logic with OR behavior
5. ✅ Updated selected filters display to show "Not categorised" instead of internal value
6. ✅ Fixed pagination to work with client-side filtering
7. ✅ Ensured full product data (title, description) is returned
8. ✅ Fixed variable naming conflict (loop variable `page` renamed to `pageNum`)

## Acceptance Criteria

### Functional Requirements
- [x] **AC1**: A "Not categorised" checkbox option appears at the bottom of the Business area filter section, separated by an "or" divider
- [x] **AC2**: A "Not categorised" checkbox option appears at the bottom of the Channel filter section, separated by an "or" divider
- [x] **AC3**: A "Not categorised" checkbox option appears at the bottom of the Phase filter section, separated by an "or" divider
- [x] **AC4**: A "Not categorised" checkbox option appears at the bottom of the Type filter section, separated by an "or" divider
- [x] **AC5**: When "Not categorised" is selected for a filter type, the results show only products that have no category value for that specific category type
- [x] **AC6**: When both a regular category value (e.g., "Phase A") and "Not categorised" are selected, results show products that match either condition (OR logic)
- [x] **AC7**: The selected filters section displays "Not categorised" (not the internal value `__not_categorised__`)
- [x] **AC8**: Products display with complete information (title, description, FIPS ID, etc.) when "Not categorised" filter is applied
- [x] **AC9**: Result counts are accurate when "Not categorised" filters are applied
- [x] **AC10**: Pagination works correctly when "Not categorised" filters are active

### Technical Requirements
- [x] **AC11**: "Not categorised" filters work correctly with keyword search
- [x] **AC12**: "Not categorised" filters work correctly when combined with other filter types (subgroups, user groups, etc.)
- [x] **AC13**: Filter removal (via selected filters tags) works correctly for "Not categorised" options
- [x] **AC14**: The filter maintains state when navigating between pages
- [x] **AC15**: No compilation errors or linting issues

### User Experience Requirements
- [x] **AC16**: The "or" divider is visually distinct and clearly separates regular options from "Not categorised"
- [x] **AC17**: The "Not categorised" label is clear and follows the same styling as other filter options
- [x] **AC18**: Selected "Not categorised" filters appear in the "Selected filters" section with clear labeling

## Testing Notes

### Manual Testing Steps
1. Navigate to `/products`
2. Expand each filter section (Business area, Channel, Phase, Type)
3. Verify "or" divider and "Not categorised" option appear at the bottom of each
4. Select "Not categorised" for Phase
5. Verify results show only products without a Phase category value
6. Verify result count is accurate
7. Verify product titles and descriptions are displayed
8. Select both a regular Phase value and "Not categorised"
9. Verify results show products matching either condition
10. Verify "Not categorised" appears correctly in selected filters section
11. Test pagination with "Not categorised" filter active
12. Test removing "Not categorised" filter via the selected filters tags

### Edge Cases Tested
- "Not categorised" selected alone
- "Not categorised" combined with regular values (OR logic)
- "Not categorised" combined with keyword search
- "Not categorised" combined with other filter types
- Pagination with "Not categorised" filters

## Related Files

- `Views/Products/Index.cshtml` - UI changes for filter options
- `Controllers/ProductsController.cs` - Backend filtering logic
- `Services/OptimizedCmsApiService.cs` - API service (no changes, but uses existing methods)

## Notes

- The internal value `__not_categorised__` is used to identify "Not categorised" selections but is never displayed to users
- When "Not categorised" filters are active, the system fetches all matching products first, then filters client-side before paginating to ensure accurate counts
- This implementation ensures products without category values are properly discoverable, improving data quality visibility


