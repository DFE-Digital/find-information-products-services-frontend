using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Models;

public class AdminProductsViewModel
{
    public List<Product> Products { get; set; } = new();
    public string UserEmail { get; set; } = string.Empty;
}

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "FIPS ID")]
    public long? FipsId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "CMDB System ID")]
    [StringLength(100, ErrorMessage = "CMDB System ID cannot exceed 100 characters")]
    public string? CmdbSysId { get; set; }

    [Required(ErrorMessage = "Short description is required")]
    [StringLength(1000, ErrorMessage = "Short description cannot exceed 1000 characters")]
    [Display(Name = "Short Description")]
    public string ShortDescription { get; set; } = string.Empty;

    [StringLength(5000, ErrorMessage = "Long description cannot exceed 5000 characters")]
    [Display(Name = "Long Description")]
    public string? LongDescription { get; set; }

    [Display(Name = "Product URL")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? ProductUrl { get; set; }

    [Required(ErrorMessage = "State is required")]
    public string State { get; set; } = "New";

    [Display(Name = "Published Date")]
    public DateTime? PublishedAt { get; set; }

    public List<string> AvailableStates => new() { "New", "Active", "Rejected", "Removed" };
}
