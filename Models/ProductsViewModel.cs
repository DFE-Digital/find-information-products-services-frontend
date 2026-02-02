using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Models
{
    public class ProductsViewModel : BaseViewModel
    {
        public ProductsViewModel()
        {
            HideNavigation = false;
            PageTitle = "Search and filter products and services";
            PageDescription = "Find information about products and services";
        }

        public List<Product> Products { get; set; } = new List<Product>();
        public List<CategoryType> CategoryTypes { get; set; } = new List<CategoryType>();
        public List<CategoryValue> CategoryValues { get; set; } = new List<CategoryValue>();
        
        // Filter properties
        public string? Keywords { get; set; }
        public List<string> SelectedPhases { get; set; } = new List<string>();
        public List<string> SelectedGroups { get; set; } = new List<string>();
        public List<string> SelectedSubgroups { get; set; } = new List<string>();
        public List<string> SelectedChannels { get; set; } = new List<string>();
        public List<string> SelectedTypes { get; set; } = new List<string>();
        public List<string> SelectedCmdbStatuses { get; set; } = new List<string>();
        public List<string> SelectedCmdbGroups { get; set; } = new List<string>();
        
        // Available filter options
        public List<FilterOption> PhaseOptions { get; set; } = new List<FilterOption>();
        public List<FilterOption> GroupOptions { get; set; } = new List<FilterOption>();
        public List<FilterOption> ChannelOptions { get; set; } = new List<FilterOption>();
        public List<FilterOption> TypeOptions { get; set; } = new List<FilterOption>();
        public List<FilterOption> CmdbStatusOptions { get; set; } = new List<FilterOption>();
        public List<FilterOption> CmdbGroupOptions { get; set; } = new List<FilterOption>();
        
        // Selected filters for display
        public List<SelectedFilter> SelectedFilters { get; set; } = new List<SelectedFilter>();
        
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages => (int)Math.Ceiling((double)FilteredCount / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartIndex => (CurrentPage - 1) * PageSize + 1;
        public int EndIndex => Math.Min(CurrentPage * PageSize, FilteredCount);
    }

    public class FilterOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsSelected { get; set; }
        public List<FilterOption>? SubOptions { get; set; }
        public string? ParentName { get; set; }
        public bool HasChildren { get; set; }
        public int ChildCount { get; set; }
    }

    public class SelectedFilter
    {
        public string Category { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public string RemoveUrl { get; set; } = string.Empty;
    }
}
