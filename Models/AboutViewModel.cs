using System.Text.Json.Serialization;

namespace FipsFrontend.Models
{
    public class PageAbout
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("meta_description")]
        public string MetaDescription { get; set; } = string.Empty;
        
        public string Body { get; set; } = string.Empty;
        
        [JsonPropertyName("related_content")]
        public string RelatedContent { get; set; } = string.Empty;
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }

    public class PageData
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("meta_description")]
        public string MetaDescription { get; set; } = string.Empty;
        
        public string Body { get; set; } = string.Empty;
        
        [JsonPropertyName("related_content")]
        public string RelatedContent { get; set; } = string.Empty;
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }

    public class PageUpdates
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("meta_description")]
        public string MetaDescription { get; set; } = string.Empty;
        
        public string Body { get; set; } = string.Empty;
        
        [JsonPropertyName("related_content")]
        public string RelatedContent { get; set; } = string.Empty;
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }

    public class PageContact
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("meta_description")]
        public string MetaDescription { get; set; } = string.Empty;
        
        public string Body { get; set; } = string.Empty;
        
        [JsonPropertyName("related_content")]
        public string RelatedContent { get; set; } = string.Empty;
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }

    public class PageHelp
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("meta_description")]
        public string MetaDescription { get; set; } = string.Empty;
        
        public string Body { get; set; } = string.Empty;
        
        [JsonPropertyName("related_content")]
        public string RelatedContent { get; set; } = string.Empty;
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }

    public class AboutViewModel : BaseViewModel
    {
        public PageAbout? PageContent { get; set; }
        public string ProcessedBody { get; set; } = "";
        public string ProcessedRelatedContent { get; set; } = "";
        
        public AboutViewModel()
        {
            HideNavigation = false; // Show navigation on about page
            PageTitle = "About this service";
            PageDescription = "Learn more about the FIPS system and how it works";
        }
    }

    public class DataViewModel : BaseViewModel
    {
        public PageData? PageContent { get; set; }
        public string ProcessedBody { get; set; } = "";
        public string ProcessedRelatedContent { get; set; } = "";
        
        public DataViewModel()
        {
            HideNavigation = false; // Show navigation on data page
            PageTitle = "Use the data";
            PageDescription = "Find out how to use or download the data in your products and services";
        }
    }

    public class UpdatesViewModel : BaseViewModel
    {
        public PageUpdates? PageContent { get; set; }
        public string ProcessedBody { get; set; } = "";
        public string ProcessedRelatedContent { get; set; } = "";
        
        public UpdatesViewModel()
        {
            HideNavigation = false; // Show navigation on updates page
            PageTitle = "Keeping information updated";
            PageDescription = "How to update information about products listed in this service";
        }
    }

    public class ContactViewModel : BaseViewModel
    {
        public PageContact? PageContent { get; set; }
        public string ProcessedBody { get; set; } = "";
        public string ProcessedRelatedContent { get; set; } = "";
        
        public ContactViewModel()
        {
            HideNavigation = false; // Show navigation on contact page
            PageTitle = "Contact us";
            PageDescription = "Get in touch with the FIPS team";
        }
    }

    public class HelpViewModel : BaseViewModel
    {
        public PageHelp? PageContent { get; set; }
        public string ProcessedBody { get; set; } = "";
        public string ProcessedRelatedContent { get; set; } = "";
        
        public HelpViewModel()
        {
            HideNavigation = false; // Show navigation on help page
            PageTitle = "Help and support";
            PageDescription = "Get help using the FIPS system";
        }
    }
}
