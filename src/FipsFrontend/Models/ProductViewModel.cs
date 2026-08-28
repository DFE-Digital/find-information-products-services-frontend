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

    public class ProductAssuranceViewModel : BaseViewModel
    {
        public Product Product { get; set; } = new Product();
        public List<ProductAssurance> ProductAssurances { get; set; } = new List<ProductAssurance>();

        public ProductAssuranceViewModel()
        {
            PageTitle = "Product Assurance";
            PageDescription = "View assurance information for this product.";
        }
    }
}
