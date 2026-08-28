using System.Net.Mail;

namespace FipsFrontend.Models;

// Bound from the "Contact" configuration section (Contact__Email as an environment variable).
public class ContactOptions
{
    public const string SectionName = "Contact";

    // The mailbox offered on the contact page for questions about the service.
    // Blank means the sentence offering it is not shown.
    // TODO: remove the default once Contact__Email is set on the hosted apps.
    public string? Email { get; set; } = "fips.service@education.gov.uk";

    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);

    // Blank is deliberately acceptable: an empty application setting must switch the sentence off, not stop the app.
    // Anything else must be a bare e-mail address; validated at start-up, see Program.cs.
    public bool IsValid() =>
        !HasEmail
        || (MailAddress.TryCreate(Email, out var address) && address.Address == Email);
}
