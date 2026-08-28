using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using find_information_products_services_tests.utilities;
using Microsoft.Playwright;
using static find_information_products_services_tests.utilities.ExcelReader;

namespace FiPSAutomation
{
    [TestFixture, Order(3)]
    [Category("Functional")]
    public class PhaseCategoryTests : BaseTest
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
        public async Task VerifyPhaseCategoryListUS35AC1()
        {
            await NavigateToAsync("categories/phase");
            await categoryDetailPage.VerifyDescriptionAsync("The stage a product or service is at in the service delivery lifecycle.");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Discovery");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Alpha");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Did not progress");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Private beta");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Public beta");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Live");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Decommissioning");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Decommissioned");
            await browseCategoriesPage.ClickBackToAllCategoriesAsync();

            ExtentTest?.Log(Status.Pass, "VerifyPhaseCategoryListUS35AC1 passed");
        }

        [Test, Order(2)]
        public async Task ClickSubcategoryLinksForPhaseCategory_US275AllAC()
        {
            List<FipsSheetRow> dataRows = ExcelReader.getRowsFromExcelFileBySheetName("testdata.xlsx", "category_phase");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Checkbox_Locator} passed");
                await NavigateToAsync(row.Product_Locator);
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                var requestTag = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(requestTag).ToBeVisibleAsync();
                await Assertions.Expect(requestTag).ToHaveTextAsync(row.Message);
                await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(row.Filter_Text_Locator, "Phase");
                await productsSearchPage.VerifyCheckboxCheckedAsync(row.Checkbox_Locator);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
                if (await productsSearchPage.DoesChevronListExistAsync())
                {
                    await productsSearchPage.VerifyProductListVisibleAsync();
                }

                    ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Checkbox_Locator}") + " passed");
            }
        }
    }
}
