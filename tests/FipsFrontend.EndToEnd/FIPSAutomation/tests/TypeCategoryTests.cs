using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using find_information_products_services_tests.utilities;
using Microsoft.Playwright;
using static find_information_products_services_tests.utilities.ExcelReader;

namespace FiPSAutomation
{
    [TestFixture, Order(6)]
    [Category("Functional")]
    public class TypeCategoryTests : BaseTest
    {
        private CategoryDetailPage categoryDetailPage = null!;
        private ProductsSearchPage productsSearchPage = null!;

        [OneTimeSetUp]
        public void InitPages()
        {
            categoryDetailPage = new CategoryDetailPage(Page);
            productsSearchPage = new ProductsSearchPage(Page);
        }

        [Test, Order(1)]
        public async Task VerifyTypeCategoryListUS29AC1()
        {
            await NavigateToAsync("categories/type");
            await categoryDetailPage.VerifyHeadingAsync("Type");
            await categoryDetailPage.VerifyDescriptionAsync("The type of service delivery and functionality provided.");
            await categoryDetailPage.VerifySubcategoryLinkAsync("API");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Campaign");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Data");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Information");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Transactional");

            ExtentTest?.Log(Status.Pass, "VerifyTypeCategoryListUS29AC1 passed");
        }

        [Test, Order(2)]
        public async Task ClickSubcategoryLinksForType_US276AllAC()
        {
            List<FipsSheetRow> dataRows = ExcelReader.getRowsFromExcelFileBySheetName("testdata.xlsx", "category_type");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Checkbox_Locator} passed");
                await NavigateToAsync(row.Product_Locator);
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                var requestTag = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(requestTag).ToBeVisibleAsync();
                await Assertions.Expect(requestTag).ToHaveTextAsync(row.Message);
                await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(row.Filter_Text_Locator, "Type");
                await productsSearchPage.VerifyCheckboxCheckedAsync(row.Checkbox_Locator);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
                await productsSearchPage.VerifyProductListVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Checkbox_Locator}") + " passed");
            }
        }
    }
}
