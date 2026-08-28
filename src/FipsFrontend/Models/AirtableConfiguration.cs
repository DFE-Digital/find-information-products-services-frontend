namespace FipsFrontend.Models;

public class AirtableConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseId { get; set; } = string.Empty;
    public string FeedbackTableName { get; set; } = "Feedback";
}
