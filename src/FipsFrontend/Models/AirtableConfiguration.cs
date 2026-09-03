namespace FipsFrontend.Models;

public class AirtableConfiguration
{
    public const string DefaultBaseUrl = "https://api.airtable.com/v0/";

    /// <summary>
    /// Where the feedback store is, up to and including the API version (the base and table are appended); empty means
    /// the Airtable service. A local or pipeline instance points this at a stand-in that answers the same paths.
    /// </summary>
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseId { get; set; } = string.Empty;
    public string FeedbackTableName { get; set; } = "Feedback";
}
