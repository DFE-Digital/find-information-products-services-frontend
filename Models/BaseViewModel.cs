namespace FipsFrontend.Models
{
    public class BaseViewModel
    {
        public bool HideNavigation { get; set; } = false;
        public string? PageTitle { get; set; }
        public string? PageDescription { get; set; }
        public string? Group { get; set; }
    }
}
