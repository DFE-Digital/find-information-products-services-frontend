using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using find_information_products_services_tests.utilities;
using Microsoft.Playwright;
using static find_information_products_services_tests.utilities.ExcelReader;

namespace FiPSAutomation
{
    [TestFixture, Order(5)]
    [Category("Functional")]
    public class BusinessAreaCategoryTests : BaseTest
    {
        private CategoryDetailPage categoryDetailPage = null!;
        private BrowseCategoriesPage browseCategoriesPage = null!;
        private ProductsSearchPage productsSearchPage = null!;

        [OneTimeSetUp]
        public void InitPages()
        {
            categoryDetailPage = new CategoryDetailPage(Page);
            browseCategoriesPage = new BrowseCategoriesPage(Page);
            productsSearchPage = new ProductsSearchPage(Page);
        }

        [Test, Order(1)]
        public async Task VerifyBusinessAreaCategoryListUS27AC1()
        {
            await NavigateToAsync("categories/business-area");
            await categoryDetailPage.VerifyDescriptionAsync("The business area or portfolio responsible for a product or service.");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Commercial");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Customer Experience and Design");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Enterprise Data");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Children and Families");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Funding and Financial Oversight");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Operations and Infrastructure");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Regional Digital Services");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Schools Digital");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Sector Facing Services");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Skills and Growth");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Strategy");
            await browseCategoriesPage.ClickBackToAllCategoriesAsync();

            ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaCategoryListAC passed");
        }

        [Test, Order(2)]
        public async Task ClickSubcategoryLinksForBusinessArea_US274AllAC()
        {
            List<FipsSheetRow> dataRows = ExcelReader.getRowsFromExcelFileBySheetName("testdata.xlsx", "category_businessarea");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Checkbox_Locator} passed");
                await NavigateToAsync(row.Product_Locator);
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                var requestTag = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(requestTag).ToBeVisibleAsync();
                await Assertions.Expect(requestTag).ToHaveTextAsync(row.Message);
                await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(row.Filter_Text_Locator, "Business area");
                await productsSearchPage.VerifyCheckboxCheckedAsync(row.Checkbox_Locator);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
                await productsSearchPage.VerifyProductListVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Checkbox_Locator}") + " passed");
            }
        }
    }
}
