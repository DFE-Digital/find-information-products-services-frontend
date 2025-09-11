using FipsFrontend.Models;

namespace FipsFrontend.Models
{
    public class ProductCategoriesViewModel : BaseViewModel
    {
        public Product Product { get; set; } = new Product();
        public List<ProductCategoryInfo> CategoryInfo { get; set; } = new List<ProductCategoryInfo>();

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
        public bool IsMultiLevel { get; set; } = false;
    }
}
