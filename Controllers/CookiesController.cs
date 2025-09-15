using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Models;
using FipsFrontend.Helpers;
using System.Collections.Generic;

namespace FipsFrontend.Controllers
{
    public class CookiesController : Controller
    {
        [HttpGet]
        public IActionResult Preferences()
        {
            var preferences = GetCookiePreferences();
            var model = new CookiePreferencesViewModel
            {
                AnalyticsAccepted = preferences.AnalyticsAccepted
            };
            
            return View(model);
        }
        
        [HttpPost]
        public IActionResult UpdatePreferences(CookiePreferencesViewModel model)
        {
            if (ModelState.IsValid)
            {
                SetCookiePreferences(model.AnalyticsAccepted);
                
                TempData["SuccessMessage"] = "Your cookie preferences have been saved.";
                
                return RedirectToAction("Preferences");
            }
            
            return View("Preferences", model);
        }
        
        [HttpPost]
        public IActionResult BannerAction(string cookies)
        {
            // Handle cookie banner form submission
            if (cookies == "accept" || cookies == "reject")
            {
                bool analyticsAccepted = cookies == "accept";
                SetCookiePreferences(analyticsAccepted);
                
                // Set a flag to trigger analytics loading on the redirected page
                if (analyticsAccepted)
                {
                    TempData["LoadAnalytics"] = "true";
                }
                
                // Return a redirect to the same page to show the confirmation message
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }
            
            return Redirect("/");
        }
        
        private CookiePreferences GetCookiePreferences()
        {
            var preferencesCookie = Request.Cookies["cookie-preferences"];
            if (!string.IsNullOrEmpty(preferencesCookie))
            {
                try
                {
                    var preferences = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(preferencesCookie);
                    if (preferences != null && preferences.ContainsKey("analytics"))
                    {
                        return new CookiePreferences
                        {
                            AnalyticsAccepted = preferences["analytics"] == "on"
                        };
                    }
                }
                catch
                {
                    // If JSON parsing fails, treat as no preferences set
                }
            }
            
            return new CookiePreferences
            {
                AnalyticsAccepted = false
            };
        }
        
        private void SetCookiePreferences(bool analyticsAccepted)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false, // JavaScript needs to read this cookie
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            };
            
            // Create the preferences object that matches the JavaScript format
            var preferences = new Dictionary<string, string>
            {
                { "analytics", analyticsAccepted ? "on" : "off" }
            };
            
            var preferencesJson = System.Text.Json.JsonSerializer.Serialize(preferences);
            Response.Cookies.Append("cookie-preferences", preferencesJson, cookieOptions);
        }
    }
    
    public class CookiePreferences
    {
        public bool AnalyticsAccepted { get; set; }
    }
}
