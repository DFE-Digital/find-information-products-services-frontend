namespace FipsFrontend.Models
{
    // The service's own pages. Their copy lives in the views, edited in this repository; nothing is
    // fetched for them at request time.

    public class AboutViewModel : BaseViewModel
    {
        public AboutViewModel()
        {
            PageTitle = "About this service";
            PageDescription = "Learn more about the FIPS system and how it works";
        }
    }

    public class DataViewModel : BaseViewModel
    {
        public DataViewModel()
        {
            PageTitle = "Use the data";
            PageDescription = "Find out how to use or download the data in your products and services";
        }
    }

    public class UpdatesViewModel : BaseViewModel
    {
        public UpdatesViewModel()
        {
            PageTitle = "Keep information updated";
            PageDescription = "How to update information about products listed in this service";
        }
    }

    public class ContactViewModel : BaseViewModel
    {
        public ContactViewModel()
        {
            PageTitle = "Contact us";
            PageDescription = "Get in touch with the FIPS team";
        }
    }

    public class HelpViewModel : BaseViewModel
    {
        public HelpViewModel()
        {
            PageTitle = "Help and support";
            PageDescription = "Get help using the FIPS system";
        }
    }
}
