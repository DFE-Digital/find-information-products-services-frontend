using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class CategoryDetailPage
    {
        private readonly IPage page;

        public CategoryDetailPage(IPage page)
        {
            this.page = page;
        }

        public async Task VerifyHeadingAsync(string heading)
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameString = heading })).ToBeVisibleAsync();
        }

        public async Task VerifyDescriptionAsync(string description)
        {
            await Assertions.Expect(page.GetByText(description, new() { Exact = true })).ToBeVisibleAsync();
        }

        public async Task VerifySubcategoryLinkAsync(string linkName)
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { NameString = linkName })).ToBeVisibleAsync();
        }

        public async Task VerifySubcategoryDescriptionAsync(string locator, string expectedText)
        {
            await Assertions.Expect(page.Locator(locator)).ToHaveTextAsync(expectedText);
        }

        public async Task ClickBackToAllCategoriesAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Back to all categories" }).ClickAsync();
        }

        public ILocator GetSubcategoryDescription(int index)
        {
            return page.Locator($"(//*[@id=\"main-content\"]/div/div/div/ul/li[{index}]/div/p)[2]");
        }
    }
}
