using FipsFrontend.Models;

namespace FipsFrontend.Models
{
    public class ProductViewModel : BaseViewModel
    {
        public Product Product { get; set; } = new Product();

        /// <summary>Where the page's links go: the listing to go back to, and the product pages the navigation points at. The CMS-backed paths unless a controller says otherwise.</summary>
        public string ListingPath { get; set; } = "/products";
        public string ProductPath { get; set; } = "/product";

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
