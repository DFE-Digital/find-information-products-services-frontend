using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using find_information_products_services_tests.utilities;
using Microsoft.Playwright;
using static find_information_products_services_tests.utilities.ExcelReader;

namespace FiPSAutomation
{
    [TestFixture, Order(2)]
    [Category("Functional")]
    public class BrowseCategoriesTests : BaseTest
    {
        private HomePage homePage = null!;
        private BrowseCategoriesPage browseCategoriesPage = null!;
        private CategoryDetailPage categoryDetailPage = null!;

        [OneTimeSetUp]
        public void InitPages()
        {
            homePage = new HomePage(Page);
            browseCategoriesPage = new BrowseCategoriesPage(Page);
            categoryDetailPage = new CategoryDetailPage(Page);
        }

        [Test, Order(1)]
        public async Task CheckBrowseCategoriesPageDescriptionUS05AC2()
        {
            await homePage.ClickCategoriesLinkAsync();
            await categoryDetailPage.VerifyHeadingAsync("Browse categories");
            await browseCategoriesPage.VerifyDescriptionAsync();
            ExtentTest?.Log(Status.Pass, "CheckBrowseCategoriesPageDescriptionUS05AC2 passed");
        }

        [Test, Order(2)]
        public async Task CheckCategoriesListAndDescriptionUS05AC3()
        {
            await browseCategoriesPage.VerifyAllCategoryLinksAsync();
            ExtentTest?.Log(Status.Pass, "CheckCategoriesListAndDescriptionUS05AC3 passed");
        }

        [Test, Order(3)]
        public async Task ClickCategoriesLinksUS05AC4()
        {
            await NavigateToAsync("categories/channel");
            await browseCategoriesPage.VerifyCategoryDescriptionAsync("The delivery channel through which a product or service is provided to users.");
            await categoryDetailPage.VerifyHeadingAsync("Channel");
            await browseCategoriesPage.ClickBackToAllCategoriesAsync();

            await NavigateToAsync("categories/business-area");
            await categoryDetailPage.VerifyHeadingAsync("Business area");
            await Page.GoBackAsync();

            await NavigateToAsync("categories/phase");
            await categoryDetailPage.VerifyHeadingAsync("Phase");
            await Page.GoBackAsync();

            await NavigateToAsync("categories/type");
            await categoryDetailPage.VerifyHeadingAsync("Type");
            await browseCategoriesPage.ClickBackToAllCategoriesAsync();

            await NavigateToAsync("categories/user-group");
            await categoryDetailPage.VerifyHeadingAsync("User group");
            await browseCategoriesPage.ClickBackToAllCategoriesAsync();
            ExtentTest?.Log(Status.Pass, "ClickCategoriesLinksUS05AC4 passed");
        }
    }
}
