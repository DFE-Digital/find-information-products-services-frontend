using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FipsFrontend.Models;

[DataClassification(DataClassification.Internal, "Proposed changes for product information")]
public class ProposedChange
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }
    [JsonPropertyName("product_document_id")]
    public string ProductDocumentId { get; set; } = string.Empty;
    [JsonPropertyName("product_fips_id")]
    public string? ProductFipsId { get; set; }
    [JsonPropertyName("product_title")]
    public string ProductTitle { get; set; } = string.Empty;
    [JsonPropertyName("submitter_email")]
    public string SubmitterEmail { get; set; } = string.Empty;
    [JsonPropertyName("submitter_name")]
    public string? SubmitterName { get; set; }
    [JsonPropertyName("date_requested")]
    public DateTime DateRequested { get; set; }
    public string Status { get; set; } = "New";
    public string? Notes { get; set; }
    [JsonPropertyName("admin_notes")]
    public string? AdminNotes { get; set; }
    
    // Proposed values
    [JsonPropertyName("proposed_title")]
    public string? ProposedTitle { get; set; }
    [JsonPropertyName("proposed_short_description")]
    public string? ProposedShortDescription { get; set; }
    [JsonPropertyName("proposed_long_description")]
    public string? ProposedLongDescription { get; set; }
    [JsonPropertyName("proposed_product_url")]
    public string? ProposedProductUrl { get; set; }
    [JsonPropertyName("proposed_category_values")]
    public List<ProposedCategoryValueChange>? ProposedCategoryValues { get; set; }
    [JsonPropertyName("proposed_product_contacts")]
    public List<ProposedContactChange>? ProposedProductContacts { get; set; }
    
    // Current values
    [JsonPropertyName("current_title")]
    public string? CurrentTitle { get; set; }
    [JsonPropertyName("current_short_description")]
    public string? CurrentShortDescription { get; set; }
    [JsonPropertyName("current_long_description")]
    public string? CurrentLongDescription { get; set; }
    [JsonPropertyName("current_product_url")]
    public string? CurrentProductUrl { get; set; }
    [JsonPropertyName("current_category_values")]
    public List<ProposedCategoryValueChange>? CurrentCategoryValues { get; set; }
    [JsonPropertyName("current_product_contacts")]
    public List<ProposedContactChange>? CurrentProductContacts { get; set; }
    
    // Action tracking
    [JsonPropertyName("actioned_by")]
    public string? ActionedBy { get; set; }
    [JsonPropertyName("actioned_date")]
    public DateTime? ActionedDate { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class ProposedCategoryValueChange
{
    [JsonPropertyName("category_type")]
    public string? CategoryType { get; set; }
    [JsonPropertyName("category_value_ids")]
    public List<int> CategoryValueIds { get; set; } = new List<int>();
    [JsonPropertyName("category_value_names")]
    public List<string> CategoryValueNames { get; set; } = new List<string>();
}

public class ProposedContactChange
{
    public string? Role { get; set; }
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }
    [JsonPropertyName("user_email")]
    public string? UserEmail { get; set; }
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }
}

// View models
public class ProposeChangeViewModel : BaseViewModel
{
    public Product Product { get; set; } = new Product();
    
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string? ProposedTitle { get; set; }
    
    [StringLength(500, ErrorMessage = "Short description cannot exceed 500 characters")]
    public string? ProposedShortDescription { get; set; }
    
    [StringLength(2000, ErrorMessage = "Long description cannot exceed 2000 characters")]
    public string? ProposedLongDescription { get; set; }
    
    [StringLength(500, ErrorMessage = "Product URL cannot exceed 500 characters")]
    public string? ProposedProductUrl { get; set; }
    
    [StringLength(2000, ErrorMessage = "User description cannot exceed 2000 characters")]
    public string? ProposedUserDescription { get; set; }
    
    [StringLength(255, ErrorMessage = "Service Owner cannot exceed 255 characters")]
    public string? ProposedServiceOwner { get; set; }
    
    [StringLength(255, ErrorMessage = "Information Asset Owner cannot exceed 255 characters")]
    public string? ProposedInformationAssetOwner { get; set; }
    
    [StringLength(255, ErrorMessage = "Delivery Manager cannot exceed 255 characters")]
    public string? ProposedDeliveryManager { get; set; }
    
    [StringLength(255, ErrorMessage = "Product Manager cannot exceed 255 characters")]
    public string? ProposedProductManager { get; set; }
    
    [Required(ErrorMessage = "Please provide a reason for this change")]
    [StringLength(2000, ErrorMessage = "Reason cannot exceed 2000 characters")]
    public string Reason { get; set; } = string.Empty;
    
    // Category values
    public int? ProposedPhaseId { get; set; }
    public int? ProposedGroupId { get; set; }
    public List<int> ProposedChannelIds { get; set; } = new List<int>();
    public List<int> ProposedTypeIds { get; set; } = new List<int>();
    
    // Available options
    public List<CategoryValue> AvailablePhases { get; set; } = new List<CategoryValue>();
    public List<CategoryValue> AvailableGroups { get; set; } = new List<CategoryValue>();
    public List<CategoryValue> AvailableChannels { get; set; } = new List<CategoryValue>();
    public List<CategoryValue> AvailableTypes { get; set; } = new List<CategoryValue>();
    
    // Product contacts
    public List<ProposedProductContactModel> ProposedProductContacts { get; set; } = new List<ProposedProductContactModel>();
    public List<UsersPermissionsUser> AvailableUsers { get; set; } = new List<UsersPermissionsUser>();
    
    public ProposeChangeViewModel()
    {
        HideNavigation = false;
        PageTitle = "Propose a change";
        PageDescription = "Suggest changes to product details, categories and contacts";
    }
}

public class ProposedProductContactModel
{
    public string? Role { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsNew { get; set; } = false;
}

public class ReviewProposedChangesViewModel : BaseViewModel
{
    public Product Product { get; set; } = new Product();
    public List<ProposedChange> ProposedChanges { get; set; } = new List<ProposedChange>();
    
    public ReviewProposedChangesViewModel()
    {
        HideNavigation = false;
        PageTitle = "Review proposed changes";
        PageDescription = "Review and action suggested changes to product information";
    }
}

public class ProposedChangeActionModel
{
    [Required]
    public string ProposedChangeId { get; set; } = string.Empty;
    
    [Required]
    public string Action { get; set; } = string.Empty; // "approve" or "reject"
    
    public List<string> FieldsToApply { get; set; } = new List<string>();
    
    [StringLength(2000, ErrorMessage = "Admin notes cannot exceed 2000 characters")]
    public string? AdminNotes { get; set; }
}

