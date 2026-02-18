using System.Text.Json.Serialization;
using System.Text.Json;

namespace FipsFrontend.Models;

public class SearchTerm
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    [JsonPropertyName("search_term")]
    public string SearchTermText { get; set; } = string.Empty;
    [JsonPropertyName("result_count")]
    public int ResultCount { get; set; }
    [JsonPropertyName("results")]
    public List<SearchTermResult>? Results { get; set; }
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }
    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class SearchTermResult
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

[DataClassification(DataClassification.Internal, "Product information for internal DfE use")]
public class Product
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("cmdb_sys_id")]
    public string? CmdbSysId { get; set; }
    [JsonPropertyName("short_description")]
    public string ShortDescription { get; set; } = string.Empty;
    [JsonPropertyName("long_description")]
    public string? LongDescription { get; set; }
    [JsonPropertyName("product_url")]
    public string? ProductUrl { get; set; }
    [JsonPropertyName("category_values")]
    public List<CategoryValue>? CategoryValues { get; set; }
    [JsonPropertyName("product_contacts")]
    public List<ProductContact>? ProductContacts { get; set; }
    [JsonPropertyName("service_owner")]
    public List<EntraUser>? ServiceOwner { get; set; }
    [JsonPropertyName("product_manager")]
    public List<EntraUser>? ProductManager { get; set; }
    [JsonPropertyName("delivery_manager")]
    public List<EntraUser>? DeliveryManager { get; set; }
    [JsonPropertyName("Information_asset_owner")]
    public List<EntraUser>? InformationAssetOwner { get; set; }
    [JsonPropertyName("senior_responsible_officer")]
    public List<EntraUser>? SeniorResponsibleOfficer { get; set; }
    [JsonPropertyName("product_assurances")]
    public List<ProductAssurance>? ProductAssurances { get; set; }
    [JsonPropertyName("cmdb_last_sync")]
    public DateTime? CmdbLastSync { get; set; }
    [JsonPropertyName("fips_id")]
    public string? FipsId { get; set; }
    public string State { get; set; } = "New";
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class EntraUser
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
}

public class CategoryValue
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    [JsonPropertyName("short_description")]
    public string? ShortDescription { get; set; }
    [JsonPropertyName("search_text")]
    public string? SearchText { get; set; }
    public bool Enabled { get; set; } = true;
    [JsonPropertyName("sort_order")]
    public int? SortOrder { get; set; }
    
    [JsonPropertyName("parent")]
    public CategoryValue? Parent { get; set; }
    
    public List<CategoryValue>? Children { get; set; }
    [JsonPropertyName("category_type")]
    public CategoryType? CategoryType { get; set; }
    public List<Product>? Products { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class CategoryType
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Body { get; set; }
    [JsonPropertyName("multi_level")]
    public bool MultiLevel { get; set; } = false;
    public bool Enabled { get; set; } = true;
    [JsonPropertyName("sort_order")]
    public int? SortOrder { get; set; }
    public List<CategoryValue>? Values { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class ProductContact
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string? Role { get; set; }
    [JsonPropertyName("users_permissions_user")]
    public UsersPermissionsUser? UsersPermissionsUser { get; set; }
    public Product? Product { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class ProductAssurance
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    [JsonPropertyName("assurance_type")]
    public string AssuranceType { get; set; } = string.Empty;
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
    [JsonPropertyName("external_url")]
    public string? ExternalUrl { get; set; }
    [JsonPropertyName("date_of_assurance")]
    public DateTime? DateOfAssurance { get; set; }
    public string? Outcome { get; set; }
    public string? Phase { get; set; }
    [JsonPropertyName("last_sync_date")]
    public DateTime? LastSyncDate { get; set; }
    public Product? Product { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class UsersPermissionsUser
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
    [JsonPropertyName("entra_id")]
    public string? EntraId { get; set; }
    public bool Confirmed { get; set; } = false;
    public bool Blocked { get; set; } = false;
    public string? Provider { get; set; }
    public Role? Role { get; set; }
    [JsonPropertyName("product_contacts")]
    public List<ProductContact>? ProductContacts { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class Role
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<Permission>? Permissions { get; set; }
    public List<UsersPermissionsUser>? Users { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class Permission
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Role? Role { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class ConfigRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

// API Response wrappers
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public ApiMeta? Meta { get; set; }
}

public class ApiCollectionResponse<T>
{
    public List<T>? Data { get; set; }
    public ApiMeta? Meta { get; set; }
}

public class ApiMeta
{
    public ApiPagination? Pagination { get; set; }
}

public class ApiPagination
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int PageCount { get; set; }
    public int Total { get; set; }
}

