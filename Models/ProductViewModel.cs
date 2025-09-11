using FipsFrontend.Models;

namespace FipsFrontend.Models
{
    public class ProductViewModel : BaseViewModel
    {
        public Product Product { get; set; } = new Product();

        public ProductViewModel()
        {
            PageTitle = "Product Details";
            PageDescription = "View detailed information about this product or service.";
        }
    }
}
