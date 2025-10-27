using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Models;

[DataClassification(DataClassification.Internal, "Request for new product entry")]
public class RequestNewEntryViewModel : BaseViewModel
{
    [Required(ErrorMessage = "Please provide a product title")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please provide a description")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Service URL cannot exceed 500 characters")]
    public string? ServiceUrl { get; set; }
    
    [StringLength(2000, ErrorMessage = "Users description cannot exceed 2000 characters")]
    public string? Users { get; set; }
    
    [StringLength(255, ErrorMessage = "Delivery Manager cannot exceed 255 characters")]
    public string? DeliveryManager { get; set; }
    
    [StringLength(255, ErrorMessage = "Product Manager cannot exceed 255 characters")]
    public string? ProductManager { get; set; }
    
    [StringLength(255, ErrorMessage = "Senior Responsible Officer cannot exceed 255 characters")]
    public string? SeniorResponsibleOfficer { get; set; }
    
    [Required(ErrorMessage = "Please provide notes about this request")]
    [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    // Category values
    public int? PhaseId { get; set; }
    public int? BusinessAreaId { get; set; }
    public List<int> ChannelIds { get; set; } = new List<int>();
    public List<int> TypeIds { get; set; } = new List<int>();
    
    // Available options from CMS
    public List<CategoryValue> AvailablePhases { get; set; } = new List<CategoryValue>();
    public List<CategoryValue> AvailableBusinessAreas { get; set; } = new List<CategoryValue>();
    public List<CategoryValue> AvailableChannels { get; set; } = new List<CategoryValue>();
    public List<CategoryValue> AvailableTypes { get; set; } = new List<CategoryValue>();
    
    public RequestNewEntryViewModel()
    {
        HideNavigation = false;
        PageTitle = "Request a new product entry";
        PageDescription = "Submit a request to add a new product to FIPS";
    }
}

