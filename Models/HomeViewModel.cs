namespace FipsFrontend.Models
{
    public class HomeViewModel : BaseViewModel
    {
        public int PublishedProductsCount { get; set; } = 0;
        public int CategoryTypesCount { get; set; } = 0;
        
    public HomeViewModel()
    {
        HideNavigation = true; // Hide navigation on homepage
        PageTitle = "Find information about products and services";
        PageDescription = "Use this service to explore what DfE delivers. Build on existing work, avoid duplication, and work more effectively across teams.";
    }
    }
}
