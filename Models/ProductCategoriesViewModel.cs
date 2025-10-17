using FipsFrontend.Models;

namespace FipsFrontend.Models
{
    public class ProductCategoriesViewModel : BaseViewModel
    {
        public Product Product { get; set; } = new Product();
        public List<ProductCategoryInfo> CategoryInfo { get; set; } = new List<ProductCategoryInfo>();
        
        // Available category values for editing
        public List<CategoryValue> AvailablePhases { get; set; } = new List<CategoryValue>();
        public List<CategoryValue> AvailableGroups { get; set; } = new List<CategoryValue>();
        public List<CategoryValue> AvailableChannels { get; set; } = new List<CategoryValue>();
        public List<CategoryValue> AvailableTypes { get; set; } = new List<CategoryValue>();
        
        // Selected category values
        public int? SelectedPhaseId { get; set; }
        public int? SelectedGroupId { get; set; }
        public List<int> SelectedChannelIds { get; set; } = new List<int>();
        public List<int> SelectedTypeIds { get; set; } = new List<int>();

        public ProductCategoriesViewModel()
        {
            PageTitle = "Product Categories";
            PageDescription = "View all categories and values assigned to this product.";
        }
    }

    public class ProductCategoryInfo
    {
        public string CategoryTypeName { get; set; } = string.Empty;
        public string CategoryTypeSlug { get; set; } = string.Empty;
        public List<string> CategoryValueNames { get; set; } = new List<string>();
        public List<string> CategoryValueSlugs { get; set; } = new List<string>();
        public List<string> CategoryValueDescriptions { get; set; } = new List<string>();
        public bool IsMultiLevel { get; set; } = false;
    }
}
