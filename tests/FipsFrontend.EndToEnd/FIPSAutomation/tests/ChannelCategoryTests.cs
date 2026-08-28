using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using find_information_products_services_tests.utilities;
using Microsoft.Playwright;
using static find_information_products_services_tests.utilities.ExcelReader;

namespace FiPSAutomation
{
    [TestFixture, Order(4)]
    [Category("Functional")]
    public class ChannelCategoryTests : BaseTest
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
        public async Task VerifyChannelCategoryListUS26AC1()
        {
            await NavigateToAsync("categories/channel");
            await categoryDetailPage.VerifyDescriptionAsync("The delivery channel through which a product or service is provided to users.");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Chat");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Email");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Face-to-face");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Native app");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Other digital media");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Phone");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Print media");
            await categoryDetailPage.VerifySubcategoryLinkAsync("SMS");
            await categoryDetailPage.VerifySubcategoryLinkAsync("Web");

            ExtentTest?.Log(Status.Pass, "VerifyChannelCategoryListUS26AC1 passed");
        }

        [Test, Order(2)]
        public async Task ClickSubcategoryLinksForChannel_US273AllAC()
        {
            List<FipsSheetRow> dataRows = ExcelReader.getRowsFromExcelFileBySheetName("testdata.xlsx", "category_channel");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Checkbox_Locator} passed");
                await NavigateToAsync(row.Product_Locator);
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                var requestTag = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(requestTag).ToBeVisibleAsync();
                await Assertions.Expect(requestTag).ToHaveTextAsync(row.Message);
                await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(row.Filter_Text_Locator, "Channel");
                await productsSearchPage.VerifyCheckboxCheckedAsync(row.Checkbox_Locator);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
              //await productsSearchPage.FilterTags.VerifyShowingResultsAsync();
                await productsSearchPage.VerifyProductListVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Checkbox_Locator}") + " passed");
            }
        }
    }
}
