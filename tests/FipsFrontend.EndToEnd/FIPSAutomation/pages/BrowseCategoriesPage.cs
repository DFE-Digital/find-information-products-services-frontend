using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class BrowseCategoriesPage
    {
        private readonly IPage page;

        public BrowseCategoriesPage(IPage page)
        {
            this.page = page;
        }

        public async Task VerifyDescriptionAsync()
        {
            await Assertions.Expect(
                page.GetByText("Find products and services by how they are categorised", new() { Exact = true })).ToBeVisibleAsync();
        }

        public async Task VerifyCategoryLinkVisibleAsync(string categoryName)
        {
            await Assertions.Expect(
                page.GetByRole(AriaRole.Link, new() { NameString = categoryName })).ToBeVisibleAsync();
        }

        public async Task VerifyCategoryDescriptionAsync(string description)
        {
            await Assertions.Expect(
                page.GetByText(description, new() { Exact = true })).ToBeVisibleAsync();
        }

        public async Task ClickBackToAllCategoriesAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Back to all categories" }).ClickAsync();
        }

        public async Task VerifyAllCategoryLinksAsync()
        {
            await VerifyCategoryLinkVisibleAsync("Channel");
            await VerifyCategoryDescriptionAsync("The delivery channel through which a product or service is provided to users.");

            await VerifyCategoryLinkVisibleAsync("Business area");
            await VerifyCategoryDescriptionAsync("The business area or portfolio responsible for a product or service.");

            await VerifyCategoryLinkVisibleAsync("Phase");
            await VerifyCategoryDescriptionAsync("The stage a product or service is at in the service delivery lifecycle.");

            await VerifyCategoryLinkVisibleAsync("Type");
            await VerifyCategoryDescriptionAsync("The type of service delivery and functionality provided.");

            await VerifyCategoryLinkVisibleAsync("User group");
            await VerifyCategoryDescriptionAsync("The users of the product or service.");
        }
    }
}
