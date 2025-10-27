using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Models
{
    public class ProductCreateViewModel : BaseViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Short description cannot exceed 500 characters")]
        public string ShortDescription { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Service Owner cannot exceed 255 characters")]
        public string? ServiceOwner { get; set; }

        [StringLength(255, ErrorMessage = "Information Asset Owner cannot exceed 255 characters")]
        public string? InformationAssetOwner { get; set; }

        [StringLength(255, ErrorMessage = "Delivery Manager cannot exceed 255 characters")]
        public string? DeliveryManager { get; set; }

        [StringLength(255, ErrorMessage = "Product Manager cannot exceed 255 characters")]
        public string? ProductManager { get; set; }

        public string State { get; set; } = "Active";

        public int? SelectedGroupId { get; set; }

        public int? SelectedPhaseId { get; set; }

        public List<CategoryValue> AvailableGroups { get; set; } = new List<CategoryValue>();

        public List<CategoryValue> AvailablePhases { get; set; } = new List<CategoryValue>();

        public ProductCreateViewModel()
        {
            HideNavigation = false;
            PageTitle = "Create new product";
            PageDescription = "Add a new product to the system";
        }
    }
}
