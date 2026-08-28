namespace FipsFrontend.Models
{
    public class CookiePreferencesViewModel
    {
        public bool AnalyticsAccepted { get; set; }
        public bool EssentialAccepted { get; set; } = true; // Always true
    }
}
