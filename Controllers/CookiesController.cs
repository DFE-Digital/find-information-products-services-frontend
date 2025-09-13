using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Models;
using FipsFrontend.Helpers;

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
        
        private CookiePreferences GetCookiePreferences()
        {
            var preferencesSet = Request.Cookies["cookie-preferences-set"];
            if (!string.IsNullOrEmpty(preferencesSet))
            {
                var analyticsAccepted = Request.Cookies["analytics-cookies-accepted"];
                return new CookiePreferences
                {
                    AnalyticsAccepted = analyticsAccepted == "true"
                };
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
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            };
            
            Response.Cookies.Append("cookie-preferences-set", "true", cookieOptions);
            Response.Cookies.Append("analytics-cookies-accepted", analyticsAccepted.ToString().ToLower(), cookieOptions);
        }
    }
    
    public class CookiePreferences
    {
        public bool AnalyticsAccepted { get; set; }
    }
}
