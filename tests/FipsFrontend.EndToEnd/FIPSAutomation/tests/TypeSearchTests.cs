using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;

namespace FiPSAutomation;

[TestFixture, Order(15)]
[Category("Functional")]
public class TypeSearchTests : BaseTest
{
    private ProductsSearchPage productsSearchPage = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productsSearchPage = new ProductsSearchPage(Page);
    }

    private async Task VerifyTypeFilterAsync(string checkboxLocator, string filterTagLocator, string expectedTagText, bool navigateFirst = false)
    {
        if (navigateFirst) 
        { 
            await NavigateToAsync("products");
        }
        await productsSearchPage.FilterPanel.VerifyTypeVisibleAsync();
        await productsSearchPage.FilterPanel.OpenTypeAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(checkboxLocator);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(filterTagLocator, expectedTagText);
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
    }

    [Test, Order(1)]
    public async Task VerifyTypeSearchFunctionality_APICategoryUS237AC1()
    {
        await VerifyTypeFilterAsync(productsSearchPage.FilterPanel.Type_API, productsSearchPage.FilterTags.Type_API, "API × Remove API filter", navigateFirst: true);
        ExtentTest?.Log(Status.Pass, "VerifyTypeSearchFunctionality_APICategoryUS237AC1 passed");
    }

    [Test, Order(2)]
    public async Task VerifyTypeSearchFunctionality_CampaignCategoryUS237AC2()
    {
        await VerifyTypeFilterAsync(productsSearchPage.FilterPanel.Type_Campaign, productsSearchPage.FilterTags.Type_Campaign, "Campaign × Remove Campaign filter");
        ExtentTest?.Log(Status.Pass, "VerifyTypeSearchFunctionality_CampaignCategoryUS237AC2 passed");
    }

    [Test, Order(3)]    
    public async Task VerifyTypeSearchFunctionality_DataCategoryUS237AC3()
    {
        await VerifyTypeFilterAsync(productsSearchPage.FilterPanel.Type_Data, productsSearchPage.FilterTags.Type_Data, "Data    × Remove Data    filter");
        ExtentTest?.Log(Status.Pass, "VerifyTypeSearchFunctionality_DataCollectionAndReportingCategoryUS237AC3 passed");
    }

    [Test, Order(4)]
    public async Task VerifyTypeSearchFunctionality_InformationCategoryUS237AC4()
    {
        await productsSearchPage.FilterPanel.VerifyTypeVisibleAsync();
        await productsSearchPage.FilterPanel.OpenTypeAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Type_Information);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_Information, "Information × Remove Information filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(2);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?type=information&page=2");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(3);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?type=information&page=3");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "VerifyTypeSearchFunctionality_InformationCategoryUS237AC4 passed");
    }

    [Test, Order(5)]
    public async Task VerifyTypeSearchFunctionality_TransactionalCategoryUS237AC5()
    {
        await productsSearchPage.FilterPanel.VerifyTypeVisibleAsync();
        await productsSearchPage.FilterPanel.OpenTypeAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Type_Transactional);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_Transactional, "Transactional × Remove Transactional filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(2);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?type=transactional&page=2");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(3);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?type=transactional&page=3");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "VerifyTypeSearchFunctionality_TransactionalCategoryUS237AC5 passed");
    }

    [Test, Order(6)]
    public async Task VerifyTypeSearchFunctionality_NotCategorisedCategoryUS237AC6()
    {
        await VerifyTypeFilterAsync(productsSearchPage.FilterPanel.Type_NotCategorised, productsSearchPage.FilterTags.Type_NotCategorised, "Not categorised × Remove Not categorised filter");
        ExtentTest?.Log(Status.Pass, "VerifyTypeSearchFunctionality_NotCategorisedCategoryUS237AC6 passed");
    }

    [Test, Order(7)]
    public async Task VerifyTypeCategorywiseSearchFunctionalityUS103AC()
    {
        await productsSearchPage.FilterPanel.VerifyTypeVisibleAsync();
        await productsSearchPage.FilterPanel.OpenTypeAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Type_API);
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Type_Information);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_API, "API × Remove API filter");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_Information, "Information × Remove Information filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.Pagination.GoToNextPageAsync();
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?type=api&type=information&page=2");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "VerifyTypeCategorywiseSearchFunctionalityUS103AC passed");
    }
}
