using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Models
{
    public class ProductCreateConfirmationViewModel : BaseViewModel
    {
        public string ProductTitle { get; set; } = string.Empty;
        
        public string ReferenceNumber { get; set; } = string.Empty;
        
        public int ProductId { get; set; }

        public ProductCreateConfirmationViewModel()
        {
            HideNavigation = false;
            PageTitle = "Product created successfully";
            PageDescription = "Your new product has been added to the system";
        }
    }
}
