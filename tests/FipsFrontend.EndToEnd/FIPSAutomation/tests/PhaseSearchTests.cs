using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;

namespace FiPSAutomation;

[TestFixture, Order(14)]
[Category("Functional")]
public class PhaseSearchTests : BaseTest
{
    private ProductsSearchPage productsSearchPage = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productsSearchPage = new ProductsSearchPage(Page);
    }

    private async Task VerifyPhaseFilterAsync(string checkboxLocator, string filterTagLocator, string expectedTagText, bool expectNoProducts = false, bool hasPagination = false, string? nextPageUrl = null, bool navigateFirst = false)
    {
        if (navigateFirst)
        {
            await NavigateToAsync("products");
        }
        await productsSearchPage.FilterPanel.VerifyPhaseVisibleAsync();
        await productsSearchPage.FilterPanel.OpenPhaseAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(checkboxLocator);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Phase_FilterHeading, "Phase");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(filterTagLocator, expectedTagText);
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        if (expectNoProducts)
        {
            await productsSearchPage.VerifyNoProductsFoundAsync();
            await productsSearchPage.VerifyClearAllFiltersDescAsync(" to see all products.");
            await productsSearchPage.ClickClearAllFiltersLinkAsync();
        }
        else
        if (await productsSearchPage.DoesChevronListExistAsync()) {
            await productsSearchPage.VerifyProductListVisibleAsync();
            if (hasPagination && nextPageUrl != null)
            {
                await productsSearchPage.Pagination.GoToNextPageAsync();
                await productsSearchPage.Pagination.VerifyUrlContainsAsync(nextPageUrl);
                await productsSearchPage.VerifyProductListVisibleAsync();
            }
            await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        }
    }

    [Test, Order(1)]   
    public async Task VerifyPhaseSearchFunctionality_DiscoveryCategoryUS236AC1()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_Discovery, productsSearchPage.FilterTags.Phase_Discovery, "Discovery × Remove Discovery filter", navigateFirst: true);
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_DiscoveryCategoryUS236AC1 passed");
    }

    [Test, Order(2)]
    public async Task VerifyPhaseSearchFunctionality_AlphaCategoryUS236AC2()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_Alpha, productsSearchPage.FilterTags.Phase_Alpha, "Alpha × Remove Alpha filter");
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_AlphaCategoryUS236AC2 passed");
    }

    [Test, Order(3)]
    public async Task VerifyPhaseSearchFunctionality_DidNotProgressCategoryUS236AC3()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_DidNotProgress, productsSearchPage.FilterTags.Phase_DidNotProgress, "Did not progress × Remove Did not progress filter");
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_DidNotProgressCategoryUS236AC3 passed");
    }

    [Test, Order(4)]
    public async Task VerifyPhaseSearchFunctionality_PrivateBetaCategoryUS236AC4()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_PrivateBeta, productsSearchPage.FilterTags.Phase_PrivateBeta, "Private beta × Remove Private beta filter");
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_PrivateBetaCategoryUS236AC4 passed");
    }

    [Test, Order(5)]
    public async Task VerifyPhaseSearchFunctionality_PublicBetaCategoryUS236AC5()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_PublicBeta, productsSearchPage.FilterTags.Phase_PublicBeta, "Public beta × Remove Public beta filter", hasPagination: true, nextPageUrl: "https://find-products-services-test.azurewebsites.net/Products?phase=public-beta&page=2");
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_PublicBetaCategoryUS236AC5 passed");
    }

    [Test, Order(6)]
    public async Task VerifyPhaseSearchFunctionality_LiveCategoryUS236AC6()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_Live, productsSearchPage.FilterTags.Phase_Live, "Live × Remove Live filter");
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_LiveCategoryUS236AC6 passed");
    }

    [Test, Order(7)]
    public async Task VerifyPhaseSearchFunctionality_DecommissioningCategoryUS236AC7()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_Decommissioning, productsSearchPage.FilterTags.Phase_Decommissioning, "Decommissioning × Remove Decommissioning filter");
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_DecommissioningCategoryUS236AC7 passed");
    }

    [Test, Order(8)]
    public async Task VerifyPhaseSearchFunctionality_DecommissionedCategoryUS236AC8()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_Decommissioned, productsSearchPage.FilterTags.Phase_Decommissioned, "Decommissioned × Remove Decommissioned filter");
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_DecommissionedCategoryUS236AC8 passed");
    }

    [Test, Order(9)]
    public async Task VerifyPhaseSearchFunctionality_NotCategorisedCategoryUS236AC9()
    {
        await VerifyPhaseFilterAsync(productsSearchPage.FilterPanel.Phase_NotCategorised, productsSearchPage.FilterTags.Phase_NotCategorised, "Not categorised × Remove Not categorised filter");
        ExtentTest?.Log(Status.Pass, "VerifyPhaseSearchFunctionality_NotCategorisedCategoryUS236AC9 passed");
    }
}
