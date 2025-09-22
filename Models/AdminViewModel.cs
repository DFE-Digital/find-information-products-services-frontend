namespace FipsFrontend.Models
{
    public class AdminViewModel : BaseViewModel
    {
        public int PublishedProductsCount { get; set; } = 0;
        public int CategoryTypesCount { get; set; } = 0;
        
        public AdminViewModel()
        {
            HideNavigation = false; // Show navigation on admin page
            PageTitle = "Admin dashboard";
            PageDescription = "Manage products, categories and system settings";
        }
    }
}
