using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class HomePage
    {
        private readonly IPage page;

        private ILocator SearchButton => page.Locator("[class='govuk-button govuk-button--start govuk-button--inverse home-cta-link']");
        private ILocator SearchProductsAndServicesButton => page.Locator("a[class=\"govuk-button govuk-button--start\"]");
        private ILocator SearchLinkDescLocator => page.Locator("ol[class = 'govuk-list govuk-list--number'] li:nth-child(1)");
        public string ServiceEmailDesc => "main[id='main-content'] p:nth-child(1)";
        public string EmailLink => "a[href='mailto:fips.service@education.gov.uk']";

        //A page object is passed as a dependency to the constructor,enabling flexible management of browser pages.
        public HomePage(IPage page)   
        {
            this.page = page;
        }

        public async Task VerifyPageTitleAsync()
        {
            await Assertions.Expect(page).ToHaveTitleAsync("Find information about products and services - FIPS");
        }

        public async Task VerifyMainHeadingAsync()
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() 
            { NameString = "Find information about products and services" })).ToBeVisibleAsync();
        }

        public async Task VerifyServiceDescriptionAsync()
        {
            await Assertions.Expect(
                page.GetByText("Use this service to explore what DfE delivers. Build on existing work, avoid duplication, and work more effectively across teams.")
            ).ToBeVisibleAsync();
        }

        public async Task VerifySearchButtonTextAsync()
        {
            await Assertions.Expect(SearchButton).ToHaveTextAsync(" Search products and services ");
        }

        public async Task ClickSearchButtonAsync()
        {
            await SearchButton.ClickAsync();
        }

        public async Task VerifyPageHeadingsAsync()
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameString = "What you can use this service for" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameString = "Use the service" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameString = "Keep information updated" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameString = "Update a product or service" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameString = "Add a missing product or service" })).ToBeVisibleAsync();
        }

        public async Task VerifySearchProductsAndServicesButtonAsync()
        {
            await Assertions.Expect(SearchProductsAndServicesButton).ToBeVisibleAsync();
        }

        public async Task ClickSearchProductsAndServicesButtonAsync()
        {
            await SearchProductsAndServicesButton.ClickAsync();
        }

        public async Task VerifySearchLinkDescription()
        {
            await Assertions.Expect(SearchLinkDescLocator).ToBeVisibleAsync();
        }

        public async Task ClickSearchLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Search" }).ClickAsync();
        }

        public async Task ClickRequestNewProductEntryLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "request a new product entry" }).ClickAsync();
        }

        public async Task ClickContactLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Contact" }).ClickAsync();
        }

        public async Task ClickBackToHomeAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Back to home" }).ClickAsync();
        }

        public async Task ClickCategoriesLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Categories" }).ClickAsync();
        }
    }
}
