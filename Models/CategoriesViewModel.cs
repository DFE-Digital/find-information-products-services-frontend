using FipsFrontend.Models;

namespace FipsFrontend.Models
{
    public class CategoriesIndexViewModel : BaseViewModel
    {
        public List<CategoryType> CategoryTypes { get; set; } = new List<CategoryType>();

        public CategoriesIndexViewModel()
        {
            PageTitle = "Browse categories";
            PageDescription = "Find products and services by how they are categorised";
        }
    }

    public class CategoriesDetailViewModel : BaseViewModel
    {
        public CategoryType CategoryType { get; set; } = new CategoryType();
        public List<CategoryValue> CategoryValues { get; set; } = new List<CategoryValue>();
        public CategoryValue? ParentCategory { get; set; }
        public List<CategoryValue>? SiblingCategories { get; set; }
        public string CurrentSlug { get; set; } = string.Empty;
        public string BreadcrumbPath { get; set; } = string.Empty;
        
        public CategoriesDetailViewModel()
        {
            HideNavigation = false;
            PageTitle = "Category details";
            PageDescription = "Browse products in this category";
        }
    }
}
