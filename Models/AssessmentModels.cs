using System.Text.Json.Serialization;

namespace FipsFrontend.Models;

[DataClassification(DataClassification.Internal, "Assessment information for internal DfE use")]
public class Assessment
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Url { get; set; }
    public List<AssessmentContact>? Contacts { get; set; }
    public List<AssessmentAttachment>? Attachments { get; set; }
}

public class AssessmentContact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class AssessmentAttachment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class AssessmentSummary
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Url { get; set; }
}

public class AssessmentsViewModel : BaseViewModel
{
    public List<AssessmentSummary> Assessments { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? SearchQuery { get; set; }
    public string? TypeFilter { get; set; }
    public string? PhaseFilter { get; set; }
    public string? StatusFilter { get; set; }
    public List<string> AvailableTypes { get; set; } = new();
    public List<string> AvailablePhases { get; set; } = new();
    public List<string> AvailableStatuses { get; set; } = new();
    public Dictionary<string, string[]> SelectedFilters { get; set; } = new();
    public new string PageTitle { get; set; } = "Service assessments";
}
