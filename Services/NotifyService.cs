using Notify.Client;
using Notify.Models.Responses;
using System.Text;

namespace FipsFrontend.Services;

public interface INotifyService
{
    Task<EmailNotificationResponse> SendProposedChangeEmailAsync(
        string fipsId,
        string fipsName,
        string entryLink,
        string changeTableHtml,
        string requestor,
        string? additionalNotes = null,
        string? cmdbSysId = null);
    
    Task<EmailNotificationResponse> SendNewEntryRequestEmailAsync(
        string requestor,
        string title,
        string description,
        string? phase,
        string? businessArea,
        string? channels,
        string? type,
        string? serviceUrl,
        string? users,
        string? deliveryManager,
        string? productManager,
        string? sro,
        string notes);
}

public class NotifyService : INotifyService
{
    private readonly NotificationClient _notifyClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotifyService> _logger;

    public NotifyService(IConfiguration configuration, ILogger<NotifyService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var apiKey = _configuration["Notify:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("GOV.UK Notify API key not configured. Please set Notify:ApiKey in appsettings.json");
        }
        
        _notifyClient = new NotificationClient(apiKey);
    }

    public async Task<EmailNotificationResponse> SendProposedChangeEmailAsync(
        string fipsId,
        string fipsName,
        string entryLink,
        string changeTableHtml,
        string requestor,
        string? additionalNotes = null,
        string? cmdbSysId = null)
    {
        try
        {
            var templateId = _configuration["Notify:Templates:changeEntry"];
            if (string.IsNullOrEmpty(templateId))
            {
                throw new InvalidOperationException("GOV.UK Notify template ID for 'changeEntry' not configured");
            }

            var recipientEmail = _configuration["Notify:FIPSMailbox"];
            if (string.IsNullOrEmpty(recipientEmail))
            {
                throw new InvalidOperationException("FIPS mailbox email address not configured. Please set Notify:FIPSMailbox in appsettings.json");
            }

            // Build the personalisation dictionary for the template
            var personalisation = new Dictionary<string, dynamic>
            {
                { "fipsid", fipsId },
                { "fipsName", fipsName },
                { "entryLink", entryLink },
                { "change", changeTableHtml },
                { "requestor", requestor }
            };

            // Add additional notes if provided
            if (!string.IsNullOrEmpty(additionalNotes))
            {
                personalisation["notes"] = additionalNotes;
            }

            // Add CMDB information
            if (!string.IsNullOrEmpty(cmdbSysId))
            {
                personalisation["cmdb"] = "This is a CMDB registered product. Any changes to the title, description or contacts will need to be made through Service Now.";
            }
            else
            {
                personalisation["cmdb"] = "This product is not registered in the CMDB. All details can be changed in the CMS.";
            }

            _logger.LogInformation("Sending proposed change notification email for FIPS ID: {FipsId} from requestor: {Requestor}", fipsId, requestor);
            
            // Send the email
            var response = await Task.Run(() => 
                _notifyClient.SendEmail(recipientEmail, templateId, personalisation));

            _logger.LogInformation("Email notification sent successfully. Notification ID: {NotificationId}", response.id);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send proposed change notification email for FIPS ID: {FipsId}", fipsId);
            throw;
        }
    }

    public static string BuildChangeTableHtml(
        string? currentTitle, string? proposedTitle,
        string? currentShortDesc, string? proposedShortDesc,
        string? currentLongDesc, string? proposedLongDesc,
        string? currentProductUrl, string? proposedProductUrl,
        List<string>? currentCategories, List<string>? proposedCategories,
        List<string>? currentContacts, List<string>? proposedContacts,
        string? currentUserDescription = null, string? proposedUserDescription = null,
        string? currentServiceOwner = null, string? proposedServiceOwner = null,
        string? currentInformationAssetOwner = null, string? proposedInformationAssetOwner = null,
        string? currentDeliveryManager = null, string? proposedDeliveryManager = null,
        string? currentProductManager = null, string? proposedProductManager = null)
    {
        var changes = new List<string>();
        
        // Title - show if different (including if user wants to clear it)
        if (!string.Equals(currentTitle, proposedTitle, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentTitle) ? "(empty)" : currentTitle;
            var proposed = string.IsNullOrWhiteSpace(proposedTitle) ? "(empty)" : proposedTitle;
            changes.Add($"Title\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // Short Description - show if different
        if (!string.Equals(currentShortDesc, proposedShortDesc, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentShortDesc) ? "(empty)" : currentShortDesc;
            var proposed = string.IsNullOrWhiteSpace(proposedShortDesc) ? "(empty)" : proposedShortDesc;
            changes.Add($"Short Description\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // Long Description - show if different
        if (!string.Equals(currentLongDesc, proposedLongDesc, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentLongDesc) ? "(empty)" : currentLongDesc;
            var proposed = string.IsNullOrWhiteSpace(proposedLongDesc) ? "(empty)" : proposedLongDesc;
            changes.Add($"Long Description\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // Product URL - show if different
        if (!string.Equals(currentProductUrl, proposedProductUrl, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentProductUrl) ? "(empty)" : currentProductUrl;
            var proposed = string.IsNullOrWhiteSpace(proposedProductUrl) ? "(empty)" : proposedProductUrl;
            changes.Add($"Product URL\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // Categories - show if different
        var currentCatsStr = currentCategories != null && currentCategories.Any() 
            ? string.Join("\n", currentCategories.Select(cat => $"• {cat}")) 
            : "(none)";
        var proposedCatsStr = proposedCategories != null && proposedCategories.Any() 
            ? string.Join("\n", proposedCategories.Select(cat => $"• {cat}")) 
            : "(none)";
        if (!string.Equals(currentCatsStr, proposedCatsStr, StringComparison.Ordinal))
        {
            changes.Add($"Categories\n\nCurrent:\n{currentCatsStr}\n\nProposed:\n{proposedCatsStr}");
        }

        // Contacts - show if different
        var currentContactsStr = currentContacts != null && currentContacts.Any() 
            ? string.Join("\n", currentContacts.Select(contact => $"• {contact}")) 
            : "(none)";
        var proposedContactsStr = proposedContacts != null && proposedContacts.Any() 
            ? string.Join("\n", proposedContacts.Select(contact => $"• {contact}")) 
            : "(none)";
        if (!string.Equals(currentContactsStr, proposedContactsStr, StringComparison.Ordinal))
        {
            changes.Add($"Contacts\n\nCurrent:\n{currentContactsStr}\n\nProposed:\n{proposedContactsStr}");
        }

        // User Description - show if different
        if (!string.Equals(currentUserDescription, proposedUserDescription, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentUserDescription) ? "(empty)" : currentUserDescription;
            var proposed = string.IsNullOrWhiteSpace(proposedUserDescription) ? "(empty)" : proposedUserDescription;
            changes.Add($"User Description\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // Senior Responsible Officer - show if different
        if (!string.Equals(currentServiceOwner, proposedServiceOwner, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentServiceOwner) ? "(empty)" : currentServiceOwner;
            var proposed = string.IsNullOrWhiteSpace(proposedServiceOwner) ? "(empty)" : proposedServiceOwner;
            changes.Add($"Senior Responsible Officer\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // Information Asset Owner - show if different
        if (!string.Equals(currentInformationAssetOwner, proposedInformationAssetOwner, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentInformationAssetOwner) ? "(empty)" : currentInformationAssetOwner;
            var proposed = string.IsNullOrWhiteSpace(proposedInformationAssetOwner) ? "(empty)" : proposedInformationAssetOwner;
            changes.Add($"Information Asset Owner\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // Delivery Manager - show if different
        if (!string.Equals(currentDeliveryManager, proposedDeliveryManager, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentDeliveryManager) ? "(empty)" : currentDeliveryManager;
            var proposed = string.IsNullOrWhiteSpace(proposedDeliveryManager) ? "(empty)" : proposedDeliveryManager;
            changes.Add($"Delivery Manager\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // Product Manager - show if different
        if (!string.Equals(currentProductManager, proposedProductManager, StringComparison.Ordinal))
        {
            var current = string.IsNullOrWhiteSpace(currentProductManager) ? "(empty)" : currentProductManager;
            var proposed = string.IsNullOrWhiteSpace(proposedProductManager) ? "(empty)" : proposedProductManager;
            changes.Add($"Product Manager\n\nCurrent:\n{current}\n\nProposed:\n{proposed}");
        }

        // If no changes, return a message
        if (!changes.Any())
        {
            return "(No changes proposed)";
        }

        // Join all changes with double line breaks for better readability
        return string.Join("\n\n---\n\n", changes);
    }


    private static string TruncateForTable(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;

        return text.Substring(0, maxLength - 3) + "...";
    }
    
    public async Task<EmailNotificationResponse> SendNewEntryRequestEmailAsync(
        string requestor,
        string title,
        string description,
        string? phase,
        string? businessArea,
        string? channels,
        string? type,
        string? serviceUrl,
        string? users,
        string? deliveryManager,
        string? productManager,
        string? sro,
        string notes)
    {
        try
        {
            var templateId = _configuration["Notify:Templates:newEntry"];
            if (string.IsNullOrEmpty(templateId))
            {
                throw new InvalidOperationException("GOV.UK Notify template ID for 'newEntry' not configured");
            }

            var recipientEmail = _configuration["Notify:FIPSMailbox"];
            if (string.IsNullOrEmpty(recipientEmail))
            {
                throw new InvalidOperationException("FIPS mailbox email address not configured. Please set Notify:FIPSMailbox in appsettings.json");
            }

            // Build the personalisation dictionary for the template
            var personalisation = new Dictionary<string, dynamic>
            {
                { "requestor", requestor },
                { "title", title },
                { "description", description },
                { "phase", phase ?? "(not specified)" },
                { "businessArea", businessArea ?? "(not specified)" },
                { "channels", channels ?? "(not specified)" },
                { "type", type ?? "(not specified)" },
                { "serviceURL", serviceUrl ?? "(not specified)" },
                { "users", users ?? "(not specified)" },
                { "deliveryManager", deliveryManager ?? "(not specified)" },
                { "productManager", productManager ?? "(not specified)" },
                { "sro", sro ?? "(not specified)" },
                { "notes", notes }
            };

            _logger.LogInformation("Sending new entry request notification email for title: {Title} from requestor: {Requestor}", title, requestor);
            
            // Send the email
            var response = await Task.Run(() => 
                _notifyClient.SendEmail(recipientEmail, templateId, personalisation));

            _logger.LogInformation("New entry request email notification sent successfully. Notification ID: {NotificationId}", response.id);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new entry request notification email for title: {Title}", title);
            throw;
        }
    }
}

