using DocumentFormat.OpenXml.Spreadsheet;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class ProductsSearchPage
    {
        private readonly IPage page;
        public FilterPanelComponent FilterPanel { get; }
        public FilterTagsComponent FilterTags { get; }
        public PaginationComponent Pagination { get; }

        private ILocator ProductsAndServicesList => page.Locator("ul.dfe-chevron-card__list");
        private ILocator MissingProductServiceDesc => page.Locator("//details[@class='govuk-details']");
        private ILocator MissingProductServiceLink => page.Locator(".govuk-details__summary-text");
        private ILocator RequestNewProductDesc => page.Locator(".govuk-details__text");
        private ILocator ClearAllFiltersDesc => page.Locator("//*[@id=\"main-content\"]/div/div/div[2]/div[3]/p[2]"); // no product found case
        private ILocator ClearAllFiltersLink => page.Locator("div[class='govuk-inset-text'] a[class='govuk-link']"); // no product found case
        private ILocator ContactUsEmailDesc => page.Locator("//*[@id=\"main-content\"]/div/div/div[1]/div/p[1]");
        private ILocator KeywordSearchTextbox => page.Locator("#keywords");
        private ILocator FilterHeader => page.Locator(".moj-filter__header-title");
        private ILocator CategoryFiltersBlock => page.Locator(".moj-filter__options");

        public ProductsSearchPage(IPage page)
        {
            this.page = page;
            FilterPanel = new FilterPanelComponent(page);
            FilterTags = new FilterTagsComponent(page);
            Pagination = new PaginationComponent(page);
        }

        public async Task VerifyProductsPageHeadingAsync()
        {
            await Assertions.Expect(page.GetByText("Search and filter products and services")).ToBeVisibleAsync();
        }

        public async Task VerifyFilterOptionsBlockAsync()
        {
            await Assertions.Expect(FilterHeader).ToHaveTextAsync("Filter");
            await Assertions.Expect(CategoryFiltersBlock).ToBeVisibleAsync();
            await Assertions.Expect(KeywordSearchTextbox).ToBeVisibleAsync();
        }

        public async Task VerifyProductsListHeadingAsync()
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { NameString = "All products and services" })).ToBeVisibleAsync();
        }

        public async Task VerifyProductListVisibleAsync()
        {
            await Assertions.Expect(ProductsAndServicesList).ToBeVisibleAsync();
        }

        public async Task VerifyMissingProductSectionVisibleAsync()
        {
            await Assertions.Expect(MissingProductServiceDesc).ToBeVisibleAsync();
        }

        public async Task VerifyNoProductsFoundAsync()
        {
            await Assertions.Expect(page.GetByText("No products found matching your filters.")).ToBeVisibleAsync();
        }

        public async Task VerifyClearAllFiltersDescAsync(string expectedText)
        {
            await Assertions.Expect(ClearAllFiltersDesc).ToContainTextAsync(expectedText);
        }

        public async Task ClickClearAllFiltersLinkAsync()
        {
            await ClearAllFiltersLink.ClickAsync();
        }

        public async Task ClickMissingProductLinkAsync()
        {
            await MissingProductServiceLink.ClickAsync();
        }

        public async Task SearchByKeywordAsync(string keyword)
        {
            await KeywordSearchTextbox.FillAsync(keyword);
        }

        public async Task VerifyContactUsEmailAsync()
        {
            await Assertions.Expect(ContactUsEmailDesc).ToBeVisibleAsync();
        }

        public async Task VerifyRequestNewProductDescAsync(string expectedText)
        {
            await Assertions.Expect(RequestNewProductDesc).ToContainTextAsync(expectedText);
        }

        public async Task ClickRequestNewProductEntryLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "request for a new product entry" }).ClickAsync();
        }

        public async Task VerifyCheckboxCheckedAsync(string checkboxLocator)
        {
            bool isChecked = await page.Locator(checkboxLocator).IsCheckedAsync();
            Assert.That(isChecked, Is.True);
        }

        public async Task<bool> DoesChevronListExistAsync()
        {
            // Returns true if 1 or more instances exist
            return await ProductsAndServicesList.CountAsync() > 0;
        }

    }
}
