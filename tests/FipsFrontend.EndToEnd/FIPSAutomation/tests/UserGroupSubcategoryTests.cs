using AventStack.ExtentReports;
using DocumentFormat.OpenXml.Spreadsheet;
using find_information_products_services_tests.utilities;
using FiPSAutomation.Pages;
using Microsoft.Playwright;
using static find_information_products_services_tests.utilities.ExcelReader;

namespace FiPSAutomation
{
    [TestFixture, Order(8)]
    [Category("Functional")]
    public class UserGroupSubcategoryTests : BaseTest
    {
        private ProductsSearchPage productsSearchPage = null!;

        [OneTimeSetUp]
        public void InitPages()
        {
            productsSearchPage = new ProductsSearchPage(Page);
        }

        [Test, Order(1)]
        public async Task ClickSubcategoryLinksForAdultLearner_US277AC1()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_AdultLearner");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }

        [Test, Order(2)]
        public async Task ClickSubcategoryLinksForCareersAdviserOrWorkCoach_US277AC2()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_CareersAdviser");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }

        [Test, Order(3)]
        public async Task ClickSubcategoryLinksForChildOrYoungPerson_US277AC3()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_ChildOrYoungPers");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }

        [Test, Order(4)]
        public async Task ClickSubcategoryLinksForDfEWorkforce_US277AC4()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_DfEWorkforce");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }

        [Test, Order(5)]
        public async Task ClickSubcategoryLinksForEmployer_US277AC6()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_Employer");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }

        [Test, Order(6)]
        public async Task ClickSubcategoryLinksForLocalAuthorityWorkforce_US277AC7()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_LAWorkforce");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }

        [Test, Order(7)]
        public async Task ClickSubcategoryLinksForNEETOrCareerSeeker_US277AC8()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_NEETOrCareerSeek");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }

        [Test, Order(8)]
        public async Task ClickSubcategoryLinksForParentOrCarer_US277AC9()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_ParentOrCarer");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }

        [Test, Order(9)]
        public async Task ClickSubcategoryLinksForProfExternalUserofDfEData_US277AC10()
        {
            List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcateg_ProfExtUserofDfEData");
            foreach (var row in dataRows)
            {
                TestContext.WriteLine($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes} passed");
                await NavigateToAsync(row.Product_Locator);
                await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = row.Heading })).ToBeVisibleAsync();
                await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
                await Assertions.Expect(Page.Locator(row.Filter_Text_Locator)).ToHaveTextAsync("User group");
                var FilterText = Page.Locator(row.Filter_Tag);
                await Assertions.Expect(FilterText).ToBeVisibleAsync();
                await Assertions.Expect(FilterText).ToHaveTextAsync(row.Message);
                await Assertions.Expect(Page.Locator(row.Selected_UserTypes_Locator)).ToHaveValueAsync(row.Selected_UserTypes);
                await productsSearchPage.VerifyMissingProductSectionVisibleAsync();

                ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
            }
        }
    }
}
