namespace FipsFrontend.Models;

// Bound from the "Feedback" configuration section (Feedback__SurveyUrl as an environment variable).
public class FeedbackOptions
{
    public const string SectionName = "Feedback";

    // The external survey linked as "Give us feedback about this service".
    // Blank means the link is not shown.
    // TODO: remove the default once Feedback__SurveyUrl is set on the hosted apps.
    public string? SurveyUrl { get; set; } = "https://dferesearch.fra1.qualtrics.com/jfe/form/SV_bHoLXsj3BfAh3ZI";

    public bool HasSurvey => !string.IsNullOrWhiteSpace(SurveyUrl);

    // Blank is deliberately acceptable: an empty application setting must switch the link off, not stop the app.
    // Anything else must be an absolute http(s) URL; validated at start-up, see Program.cs.
    public bool IsValid() =>
        !HasSurvey
        || (Uri.TryCreate(SurveyUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}
