using Microsoft.Playwright;

namespace FiPSAutomation.Components
{
    public class HeaderComponent
    {
        private readonly IPage page;

        public HeaderComponent(IPage page)
        {
            this.page = page;
        }

        public async Task ClickProductsLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Products" }).ClickAsync();
        }

        public async Task ClickServiceNameLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Find information about products and services" }).ClickAsync();
        }
    }
}
