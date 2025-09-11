using Markdig;
using System.Text.RegularExpressions;

namespace FipsFrontend.Helpers
{
    public static class GovUkMarkdownHelper
    {
        public static string ToGovUkHtml(string markdown)
        {
            try
            {
                if (string.IsNullOrEmpty(markdown))
                    return string.Empty;

                // Convert markdown to HTML
                var html = Markdown.ToHtml(markdown);
                
                // Apply GOV.UK classes
                html = ApplyGovUkClasses(html);
                
                return html;
            }
            catch (Exception ex)
            {
                // Return error information for debugging
                return $"<div class=\"govuk-error-summary\"><h2 class=\"govuk-error-summary__title\">Markdown Processing Error</h2><div class=\"govuk-error-summary__body\"><p>Error: {ex.Message}</p><p>Stack: {ex.StackTrace}</p></div></div>";
            }
        }

        private static string ApplyGovUkClasses(string html)
        {
            // Apply GOV.UK heading classes
            html = Regex.Replace(html, @"<h1(?![^>]*class=)[^>]*>", "<h1 class=\"govuk-heading-xl\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<h2(?![^>]*class=)[^>]*>", "<h2 class=\"govuk-heading-l\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<h3(?![^>]*class=)[^>]*>", "<h3 class=\"govuk-heading-m\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<h4(?![^>]*class=)[^>]*>", "<h4 class=\"govuk-heading-s\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<h5(?![^>]*class=)[^>]*>", "<h5 class=\"govuk-heading-s\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<h6(?![^>]*class=)[^>]*>", "<h6 class=\"govuk-heading-s\">", RegexOptions.IgnoreCase);

            // Apply GOV.UK paragraph classes (only if no class exists)
            html = Regex.Replace(html, @"<p(?![^>]*class=)[^>]*>", "<p class=\"govuk-body\">", RegexOptions.IgnoreCase);

            // Apply GOV.UK link classes - more robust handling
            html = Regex.Replace(html, @"<a(?![^>]*class=)([^>]*?)href=""([^""]*?)""([^>]*?)>", 
                "<a$1href=\"$2\"$3 class=\"govuk-link\">", RegexOptions.IgnoreCase);

            // Apply GOV.UK list classes - handle both bullet and non-bullet lists
            html = Regex.Replace(html, @"<ul(?![^>]*class=)[^>]*>", "<ul class=\"govuk-list govuk-list--bullet\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<ol(?![^>]*class=)[^>]*>", "<ol class=\"govuk-list govuk-list--number\">", RegexOptions.IgnoreCase);

            // Apply GOV.UK table classes
            html = Regex.Replace(html, @"<table(?![^>]*class=)[^>]*>", "<table class=\"govuk-table\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<thead(?![^>]*class=)[^>]*>", "<thead class=\"govuk-table__head\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<tbody(?![^>]*class=)[^>]*>", "<tbody class=\"govuk-table__body\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<tr(?![^>]*class=)[^>]*>", "<tr class=\"govuk-table__row\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<th(?![^>]*class=)[^>]*>", "<th class=\"govuk-table__header\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<td(?![^>]*class=)[^>]*>", "<td class=\"govuk-table__cell\">", RegexOptions.IgnoreCase);

            // Apply GOV.UK blockquote classes
            html = Regex.Replace(html, @"<blockquote(?![^>]*class=)[^>]*>", "<blockquote class=\"govuk-inset-text\">", RegexOptions.IgnoreCase);

            // Apply GOV.UK code classes
            html = Regex.Replace(html, @"<code(?![^>]*class=)[^>]*>", "<code class=\"govuk-code\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<pre(?![^>]*class=)[^>]*>", "<pre class=\"govuk-code\">", RegexOptions.IgnoreCase);

            // Apply GOV.UK text formatting classes
            html = Regex.Replace(html, @"<strong(?![^>]*class=)[^>]*>", "<strong class=\"govuk-!-font-weight-bold\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<em(?![^>]*class=)[^>]*>", "<em class=\"govuk-!-font-style-italic\">", RegexOptions.IgnoreCase);

            // Apply GOV.UK horizontal rule
            html = Regex.Replace(html, @"<hr(?![^>]*class=)[^>]*>", "<hr class=\"govuk-section-break govuk-section-break--visible\">", RegexOptions.IgnoreCase);

            // Handle definition lists
            html = Regex.Replace(html, @"<dl(?![^>]*class=)[^>]*>", "<dl class=\"govuk-summary-list\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<dt(?![^>]*class=)[^>]*>", "<dt class=\"govuk-summary-list__key\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<dd(?![^>]*class=)[^>]*>", "<dd class=\"govuk-summary-list__value\">", RegexOptions.IgnoreCase);

            // Handle address elements
            html = Regex.Replace(html, @"<address(?![^>]*class=)[^>]*>", "<address class=\"govuk-body\">", RegexOptions.IgnoreCase);

            // Handle small text
            html = Regex.Replace(html, @"<small(?![^>]*class=)[^>]*>", "<small class=\"govuk-body-s\">", RegexOptions.IgnoreCase);

            return html;
        }

        /// <summary>
        /// Creates a plain unordered list without bullets (useful for navigation or simple lists)
        /// </summary>
        public static string ToGovUkPlainList(string markdown)
        {
            try
            {
                if (string.IsNullOrEmpty(markdown))
                    return string.Empty;

                var html = Markdown.ToHtml(markdown);
                
                // Apply GOV.UK classes but use plain list styling
                html = ApplyGovUkClasses(html);
                
                // Override ul class to remove bullets
                html = Regex.Replace(html, @"class=""govuk-list govuk-list--bullet""", "class=\"govuk-list\"", RegexOptions.IgnoreCase);
                
                return html;
            }
            catch (Exception ex)
            {
                return $"<div class=\"govuk-error-summary\"><h2 class=\"govuk-error-summary__title\">Markdown Processing Error</h2><div class=\"govuk-error-summary__body\"><p>Error: {ex.Message}</p></div></div>";
            }
        }

        /// <summary>
        /// Creates a definition list styled as GOV.UK summary list
        /// </summary>
        public static string ToGovUkSummaryList(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var html = Markdown.ToHtml(markdown);
            
            // Apply GOV.UK classes
            html = ApplyGovUkClasses(html);
            
            // Convert definition lists to summary list format
            html = Regex.Replace(html, @"<dl class=""govuk-summary-list"">(.*?)</dl>", 
                "<dl class=\"govuk-summary-list\">$1</dl>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            return html;
        }
    }
}
