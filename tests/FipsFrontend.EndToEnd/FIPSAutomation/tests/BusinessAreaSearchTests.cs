using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;

namespace FiPSAutomation;

[TestFixture, Order(12)]
[Category("Functional")]
public class BusinessAreaSearchTests : BaseTest
{
    private ProductsSearchPage productsSearchPage = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productsSearchPage = new ProductsSearchPage(Page);
    }

    private async Task VerifyBusinessAreaFilterAsync(string checkboxLocator, string filterTagLocator, string expectedTagText, bool navigateFirst = false, bool hasPagination = false, int? page2 = null, int? page3 = null, string? page2Url = null, string? page3Url = null)
    {
        if (navigateFirst)
        {
            await NavigateToAsync("products");
        }
        await productsSearchPage.FilterPanel.VerifyBusinessAreaVisibleAsync();
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(checkboxLocator);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await Task.Delay(1000);
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(filterTagLocator, expectedTagText);
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        if (hasPagination && page2.HasValue && page2Url != null)
        {
            await productsSearchPage.Pagination.GoToPageAsync(page2.Value);
            await productsSearchPage.Pagination.VerifyUrlContainsAsync(page2Url);
            await productsSearchPage.VerifyProductListVisibleAsync();
            if (page3.HasValue && page3Url != null)
            {
                await productsSearchPage.Pagination.GoToPageAsync(page3.Value);
                await productsSearchPage.Pagination.VerifyUrlContainsAsync(page3Url);
                await productsSearchPage.VerifyProductListVisibleAsync();
            }
            else
            {
                await productsSearchPage.Pagination.GoToNextPageAsync();
            }
        }
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
    }

    [Test, Order(1)]           
    public async Task VerifyBusinessAreaSearchFunctionality_CommercialCategoryUS234AC1()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_Commercial, productsSearchPage.FilterTags.BA_Commercial, "Commercial × Remove Commercial filter", navigateFirst: true);
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaCategorywiseSearchFunctionalityUS234AC1 passed");
    }

    [Test, Order(2)]
    public async Task VerifyBusinessAreaSearchFunctionality_CustomerExpAndDesignUS234AC2()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_CustomerExpAndDesign, productsSearchPage.FilterTags.BA_CustomerExpAndDesign, "Customer Experience and Design × Remove Customer Experience and Design filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_CustomerExpAndDesignUS234AC2 passed");
    }

    [Test, Order(3)]
    public async Task VerifyBusinessAreaSearchFunctionality_ChildrenAndFamiliesUS234AC3()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_ChildrenAndFamilies, productsSearchPage.FilterTags.BA_ChildrenAndFamilies, "Children and Families × Remove Children and Families filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_ChildrenAndFamiliesUS234AC3 passed");
    }

    [Test, Order(4)]
    public async Task VerifyBusinessAreaSearchFunctionality_EnterpriseDataUS234AC4()
    {
        await NavigateToAsync("products");
        await productsSearchPage.FilterPanel.VerifyBusinessAreaVisibleAsync();
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_EnterpriseData);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await Task.Delay(1000);
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_EnterpriseData, "Enterprise Data × Remove Enterprise Data filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(2);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?group=enterprise-data&page=2");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToNextPageAsync();
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?group=enterprise-data&page=3");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_EnterpriseDataUS234AC4 passed");
    }

    [Test, Order(5)]
    public async Task VerifyBusinessAreaSearchFunctionality_FundingAndFinancialOversightUS234AC5()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_FundingAndFinancialOversight, productsSearchPage.FilterTags.BA_FundingAndFinancialOversight, "Funding and Financial Oversight × Remove Funding and Financial Oversight filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_FundingAndFinancialOversightUS234AC5 passed");
    }

    [Test, Order(6)]
    public async Task VerifyBusinessAreaSearchFunctionality_OperationsAndInfraUS234AC6()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_OperationsAndInfra, productsSearchPage.FilterTags.BA_OperationsAndInfra, "Operations and Infrastructure × Remove Operations and Infrastructure filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_OperationsAndInfraUS234AC6 passed");
    }

    [Test, Order(7)]
    public async Task VerifyBusinessAreaSearchFunctionality_RegionalDigitalServicesUS234AC7()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_RegionalDigitalServices, productsSearchPage.FilterTags.BA_RegionalDigitalServices, "Regional Digital Services × Remove Regional Digital Services filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_RegionalDigitalServicesUS234AC7 passed");
    }

    [Test, Order(8)]
    public async Task VerifyBusinessAreaSearchFunctionality_SchoolsDigitalUS234AC8()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_SchoolsDigital, productsSearchPage.FilterTags.BA_SchoolsDigital, "Schools Digital × Remove Schools Digital filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_SchoolsDigitalUS234AC8 passed");
    }

    [Test, Order(9)]
    public async Task VerifyBusinessAreaSearchFunctionality_SectorFacingServicesUS234AC9()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_SectorFacingServices, productsSearchPage.FilterTags.BA_SectorFacingServices, "Sector Facing Services × Remove Sector Facing Services filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_SectorFacingServicesUS234AC9 passed");
    }

    [Test, Order(10)]
    public async Task VerifyBusinessAreaSearchFunctionality_SkillsAndGrowthUS234AC10()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_SkillsAndGrowth, productsSearchPage.FilterTags.BA_SkillsAndGrowth, "Skills and Growth × Remove Skills and Growth filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_SkillsAndGrowthUS234AC10 passed");
    }

    [Test, Order(11)]
    public async Task VerifyBusinessAreaSearchFunctionality_StrategyCategoryUS234AC11()
    {
        await VerifyBusinessAreaFilterAsync(productsSearchPage.FilterPanel.BA_Strategy, productsSearchPage.FilterTags.BA_Strategy, "Strategy × Remove Strategy filter");
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_StrategyCategoryUS234AC11 passed");
    }

    [Test, Order(12)]
    public async Task VerifyBusinessAreaSearchFunctionality_NotCategorisedUS234AC12()
    {
        await NavigateToAsync("products");
        await productsSearchPage.FilterPanel.VerifyBusinessAreaVisibleAsync();
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_NotCategorised);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await Task.Delay(1000);
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_NotCategorised, "Not categorised × Remove Not categorised filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(2);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?group=__not_categorised__&page=2");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(3);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?group=__not_categorised__&page=3");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "VerifyBusinessAreaSearchFunctionality_NotCategorisedUS234AC12 passed");
    }
}
