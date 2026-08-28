using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Models
{
    public class ProductEditViewModel : BaseViewModel
    {
        public Product Product { get; set; } = new Product();

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Short description cannot exceed 500 characters")]
        public string ShortDescription { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Long description cannot exceed 2000 characters")]
        public string? LongDescription { get; set; }

        // Category values
        public int? SelectedPhaseId { get; set; }
        public int? SelectedGroupId { get; set; }
        public List<int> SelectedChannelIds { get; set; } = new List<int>();
        public List<int> SelectedTypeIds { get; set; } = new List<int>();

        // Available options
        public List<CategoryValue> AvailablePhases { get; set; } = new List<CategoryValue>();
        public List<CategoryValue> AvailableGroups { get; set; } = new List<CategoryValue>();
        public List<CategoryValue> AvailableChannels { get; set; } = new List<CategoryValue>();
        public List<CategoryValue> AvailableTypes { get; set; } = new List<CategoryValue>();

        // Product contacts
        public List<ProductContactEditModel> ProductContacts { get; set; } = new List<ProductContactEditModel>();
        public List<UsersPermissionsUser> AvailableUsers { get; set; } = new List<UsersPermissionsUser>();

        public ProductEditViewModel()
        {
            HideNavigation = false;
            PageTitle = "Edit product";
            PageDescription = "Edit product details, categories and contacts";
        }
    }

    public class ProductContactEditModel
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public int? UserId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsNew { get; set; } = false;
    }
}
