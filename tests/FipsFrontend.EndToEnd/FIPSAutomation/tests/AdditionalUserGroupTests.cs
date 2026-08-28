using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using find_information_products_services_tests.utilities;
using Microsoft.Playwright;
using static find_information_products_services_tests.utilities.ExcelReader;

namespace FiPSAutomation;

[TestFixture, Order(18)]
[Category("Functional")]
public class AdditionalUserGroupTests : BaseTest
{
    private ProductsSearchPage productsSearchPage = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productsSearchPage = new ProductsSearchPage(Page);
    }

    [Test, Order(1)]
    public async Task VerifyUGSocialWorkerSubcategoryListUS101AC()
    {
        List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_SocialWorker_list");
        if (dataRows.Count > 1)
        {
            await NavigateToAsync(dataRows[0].Col1);
            await Assertions.Expect(Page.Locator(dataRows[0].Col2)).ToHaveTextAsync("Social care workforce - Social worker");
            await Assertions.Expect(Page.GetByText(dataRows[0].Col3, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(dataRows[1].Col2)).ToHaveTextAsync(dataRows[1].Col3);
            await Assertions.Expect(Page.Locator(dataRows[2].Col2)).ToHaveTextAsync(dataRows[2].Col3);

            for (int i = 1; i < dataRows.Count; i++)
            {
                if (dataRows[i].Col1 != "")
                {
                    await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = dataRows[i].Col1 })).ToBeVisibleAsync();
                }
            }
            ExtentTest?.Log(Status.Pass, "VerifyUGSocialWorkerSubcategoryListUS101AC passed");
        }
    }

    [Test, Order(2)]
    public async Task ClickSubcategoryLinksForSocialCareWorkforce_US277AC11()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_SCWorkforce");
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
    public async Task ClickSubcategoryLinksForSocialWorker_US277AC11()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_SocialWorker");
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
    public async Task ClickSubcategoryLinksForEPAndEYWorkforce_US277AC5()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "UGSubcategory_EPandEYWorkforce");
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
            await productsSearchPage.VerifyProductListVisibleAsync();

            ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
        }
    }

    [Test, Order(5)]
    public async Task VerifyUGEPEYAcademyAndTrustWorkforceSubcategoryListUS130AC()
    {
        List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "UG_EPEYAcademyTruWorkforce_list");
        if (dataRows.Count > 1)
        {
            await NavigateToAsync(dataRows[0].Col1);
            await Assertions.Expect(Page.Locator(dataRows[0].Col2)).ToHaveTextAsync("Education provider and early years workforce - Academy and trust workforce");
            await Assertions.Expect(Page.GetByText(dataRows[0].Col3, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(dataRows[1].Col2)).ToHaveTextAsync(dataRows[1].Col3);
            await Assertions.Expect(Page.Locator(dataRows[2].Col2)).ToHaveTextAsync(dataRows[2].Col3);

            for (int i = 1; i < dataRows.Count; i++)
            {
                if (dataRows[i].Col1 != "")
                {
                    await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = dataRows[i].Col1 })).ToBeVisibleAsync();
                }
            }
            ExtentTest?.Log(Status.Pass, "VerifyUGEPEYAcademyAndTrustWorkforceSubcategoryListUS130AC passed");
        }
    }

    [Test, Order(6)]
    public async Task ClickSubcategoryLinksForAcademyAndTrustWorkforce_EPEY_US277AC5()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "EPEYSubcateg_AcademyTWorkforce");
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
            await productsSearchPage.VerifyProductListVisibleAsync();

            ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
        }
    }

    [Test, Order(7)]
    public async Task VerifyUGEPEYAlternatProvSettingWorkforceSubcategListUS131AC()
    {
        List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "UG_EPEYAltProvSetWorkforce_list");
        if (dataRows.Count > 1)
        {
            await NavigateToAsync(dataRows[0].Col1);
            await Assertions.Expect(Page.Locator(dataRows[0].Col2)).ToHaveTextAsync("Education provider and early years workforce - Alternative provision setting workforce");
            await Assertions.Expect(Page.GetByText(dataRows[0].Col3, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(dataRows[1].Col2)).ToHaveTextAsync(dataRows[1].Col3);
            await Assertions.Expect(Page.Locator(dataRows[2].Col2)).ToHaveTextAsync(dataRows[2].Col3);

            for (int i = 1; i < dataRows.Count; i++)
            {
                if (dataRows[i].Col1 != "")
                {
                    await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = dataRows[i].Col1 })).ToBeVisibleAsync();
                }
            }
            ExtentTest?.Log(Status.Pass, "VerifyUGEPEYAlternatProvSettingWorkforceSubcategListUS131AC passed");
        }
    }

    [Test, Order(8)]
    public async Task ClickSubcategoryLinksForAlternatProvSettingWorkforce_EPEY_US277AC5()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "EPEYSubcateg_AltProSetWorkforce");
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
    public async Task VerifyUGEPEYEarlyYearsWorkforceSubcategoryListUS132AC()
    {
        List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "UG_EPEYEarlyYearsWorkforce_list");
        if (dataRows.Count > 1)
        {
            await NavigateToAsync(dataRows[0].Col1);
            await Assertions.Expect(Page.Locator(dataRows[0].Col2)).ToHaveTextAsync("Education provider and early years workforce - Early years workforce");
            await Assertions.Expect(Page.GetByText(dataRows[0].Col3, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(dataRows[1].Col2)).ToHaveTextAsync(dataRows[1].Col3);
            await Assertions.Expect(Page.Locator(dataRows[2].Col2)).ToHaveTextAsync(dataRows[2].Col3);

            for (int i = 1; i < dataRows.Count; i++)
            {
                if (dataRows[i].Col1 != "")
                {
                    await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = dataRows[i].Col1 })).ToBeVisibleAsync();
                }
            }
            ExtentTest?.Log(Status.Pass, "VerifyUGEPEYEarlyYearsWorkforceSubcategoryListUS132AC passed");
        }
    }

    [Test, Order(10)]
    public async Task ClickSubcategoryLinksForEarlyYearsWorkforce_EPEY_US277AC5()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "EPEYSubcatg_EarlyYearsWorkforce");
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
            await productsSearchPage.VerifyProductListVisibleAsync();

            ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
        }
    }

    [Test, Order(11)]
    public async Task VerifyUGEPEYFurtherEducationWorkforceSubcategoryListUS133AC()
    {
        List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "UG_EPEYFurtherEduWorkforce_list");
        if (dataRows.Count > 1)
        {
            await NavigateToAsync(dataRows[0].Col1);
            await Assertions.Expect(Page.Locator(dataRows[0].Col2)).ToHaveTextAsync("Education provider and early years workforce - Further education workforce");
            await Assertions.Expect(Page.GetByText(dataRows[0].Col3, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(dataRows[1].Col2)).ToHaveTextAsync(dataRows[1].Col3);
            await Assertions.Expect(Page.Locator(dataRows[2].Col2)).ToHaveTextAsync(dataRows[2].Col3);

            for (int i = 1; i < dataRows.Count; i++)
            {
                if (dataRows[i].Col1 != "")
                {
                    await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = dataRows[i].Col1 })).ToBeVisibleAsync();
                }
            }
            ExtentTest?.Log(Status.Pass, "VerifyUGEPEYFurtherEducationWorkforceSubcategoryListUS133AC passed");
        }
    }

    [Test, Order(12)]
    public async Task ClickSubcategoryLinksForFurtherEducationWorkforce_EPEY_US277AC5()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "EPEYSubcatg_FurtherEduWorkforce");
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

    [Test, Order(13)]
    public async Task VerifyUGEPEYHigherEducationWorkforceSubcategoryListUS134AC()
    {
        List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "UG_EPEYHigherEduWorkforce_list");
        if (dataRows.Count > 1)
        {
            await NavigateToAsync(dataRows[0].Col1);
            await Assertions.Expect(Page.Locator(dataRows[0].Col2)).ToHaveTextAsync("Education provider and early years workforce - Higher education workforce");
            await Assertions.Expect(Page.GetByText(dataRows[0].Col3, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(dataRows[1].Col2)).ToHaveTextAsync(dataRows[1].Col3);
            await Assertions.Expect(Page.Locator(dataRows[2].Col2)).ToHaveTextAsync(dataRows[2].Col3);

            for (int i = 1; i < dataRows.Count; i++)
            {
                if (dataRows[i].Col1 != "")
                {
                    await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = dataRows[i].Col1 })).ToBeVisibleAsync();
                }
            }
            ExtentTest?.Log(Status.Pass, "VerifyUGEPEYHigherEducationWorkforceSubcategoryListUS134AC passed");
        }
    }

    [Test, Order(14)]
    public async Task ClickSubcategoryLinksForHigherEducationWorkforce_EPEY_US277AC5()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "EPEYSubcatg_HigherEduWorkforce");
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

    [Test, Order(15)]
    public async Task VerifyUGEPEY_SENDProfessionalSubcategoryListUS135AC()
    {
        List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "UG_EPEY_SENDProfessional_list");
        if (dataRows.Count > 1)
        {
            await NavigateToAsync(dataRows[0].Col1);
            await Assertions.Expect(Page.Locator(dataRows[0].Col2)).ToHaveTextAsync("Education provider and early years workforce - SEND professional");
            await Assertions.Expect(Page.GetByText(dataRows[0].Col3, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(dataRows[1].Col2)).ToHaveTextAsync(dataRows[1].Col3);
            await Assertions.Expect(Page.Locator(dataRows[2].Col2)).ToHaveTextAsync(dataRows[2].Col3);

            for (int i = 1; i < dataRows.Count; i++)
            {
                if (dataRows[i].Col1 != "")
                {
                    await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = dataRows[i].Col1 })).ToBeVisibleAsync();
                }
            }
            ExtentTest?.Log(Status.Pass, "VerifyUGEPEY_SENDProfessionalSubcategoryListUS135AC passed");
        }
    }

    [Test, Order(16)]
    public async Task ClickSubcategoryLinksForSENDProfessional_EPEY_US277AC5()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "EPEYSubcateg_SENDProfessional");
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

    [Test, Order(17)]
    public async Task VerifyUGEPEYSchoolWorkforceSubcategoryListUS136AC()
    {
        List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "UG_EPEY_SchoolWorkforce_list");
        if (dataRows.Count > 1)
        {
            await NavigateToAsync(dataRows[0].Col1);
            await Assertions.Expect(Page.Locator(dataRows[0].Col2)).ToHaveTextAsync("Education provider and early years workforce - School workforce");
            await Assertions.Expect(Page.GetByText(dataRows[0].Col3, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(dataRows[1].Col2)).ToHaveTextAsync(dataRows[1].Col3);
            await Assertions.Expect(Page.Locator(dataRows[2].Col2)).ToHaveTextAsync(dataRows[2].Col3);

            for (int i = 1; i < dataRows.Count; i++)
            {
                if (dataRows[i].Col1 != "")
                {
                    await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = dataRows[i].Col1 })).ToBeVisibleAsync();
                }
            }
            ExtentTest?.Log(Status.Pass, "VerifyUGEPEYSchoolWorkforceSubcategoryListUS136AC passed");
        }
    }

    [Test, Order(18)]
    public async Task ClickSubcategoryLinksForSchoolWorkforce_EPEY_US277AC5()
    {
        List<FipsSheetRowUG> dataRows = ExcelReader.getRowsFromExcelForSelectedUserType("testdata.xlsx", "EPEYSubcateg_SchoolWorkforce");
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
            await productsSearchPage.VerifyProductListVisibleAsync();

            ExtentTest?.Log(Status.Pass, ($"Running test for: Product={row.Product_Locator}, Filter={row.Selected_UserTypes}") + " passed");
        }
    }
}
