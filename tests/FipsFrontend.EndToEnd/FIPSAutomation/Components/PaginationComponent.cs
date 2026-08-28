using Microsoft.Playwright;

namespace FiPSAutomation.Components
{
    public class PaginationComponent
    {
        private readonly IPage page;

        private ILocator NextPageLink => page.Locator("//span[@class ='govuk-pagination__link-title' and contains(text(), 'Next')]");

        public PaginationComponent(IPage page)
        {
            this.page = page;
        }

        public async Task GoToPageAsync(int pageNumber)
        {
            await page.Locator($"a[aria-label = \"Page {pageNumber}\"]").ClickAsync();
        }

        public async Task GoToNextPageAsync()
        {
            await NextPageLink.ClickAsync();
        }

        public async Task VerifyUrlContainsAsync(string expectedUrl)
        {
            await Assertions.Expect(page).ToHaveURLAsync(expectedUrl);
        }
    }
}
