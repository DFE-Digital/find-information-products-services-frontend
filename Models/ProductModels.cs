using System.Text.Json.Serialization;

namespace FipsFrontend.Models;

[DataClassification(DataClassification.Internal, "Product information for internal DfE use")]
public class Product
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    [JsonPropertyName("fips_id")]
    public string? FipsId { get; set; }
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
    [JsonPropertyName("product_assurances")]
    public List<ProductAssurance>? ProductAssurances { get; set; }
    [JsonPropertyName("cmdb_last_sync")]
    public DateTime? CmdbLastSync { get; set; }
    public string State { get; set; } = "New";
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}

public class CategoryValue
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
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
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Role { get; set; }
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
