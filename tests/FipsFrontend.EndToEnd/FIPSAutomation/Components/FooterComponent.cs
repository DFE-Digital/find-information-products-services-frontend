using Microsoft.Playwright;

namespace FiPSAutomation.Components
{
    public class FooterComponent
    {
        private readonly IPage page;

        public FooterComponent(IPage page)
        {
            this.page = page;
        }

        public async Task ClickCookiesLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Cookies" }).ClickAsync();
        }

        public async Task ClickAccessibilityStatementAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Accessibility statement" }).ClickAsync();
        }

        public async Task ClickPrivacyPolicyAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Privacy policy" }).ClickAsync();
        }
    }
}
