using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(20)]
[Category("Functional")]
public class NotCategorisedFilterTests : BaseTest
{
    private ProductsSearchPage productsSearchPage = null!;
    private FilterPanelComponent filterPanel = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productsSearchPage = new ProductsSearchPage(Page);
        filterPanel = new FilterPanelComponent(Page);
    }

    [Test, Order(4)]
    public async Task ValidateNotCategorisedFilterOptions_SearchFunctionalityUS213AC()
    {
        await NavigateToAsync("products");
        await filterPanel.OpenBusinessAreaAsync();
        await filterPanel.CheckFilterAsync(filterPanel.BA_NotCategorised);
        await filterPanel.OpenChannelAsync();
        await filterPanel.CheckFilterAsync(filterPanel.Channel_NotCategorised);
        await filterPanel.OpenTypeAsync();
        await filterPanel.CheckFilterAsync(filterPanel.Type_NotCategorised);
        await filterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("results for your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Channel_FilterHeading, "Channel");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Channel_NotCategorisedGroup, "Not categorised × Remove Not categorised filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_NotCategorisedGroup, "Not categorised × Remove Not categorised filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_NotCategorisedGroup, "Not categorised × Remove Not categorised filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        if (await productsSearchPage.DoesChevronListExistAsync())
        { await productsSearchPage.VerifyProductListVisibleAsync();
        }
        await filterPanel.ClearAllFiltersAsync();

        ExtentTest?.Log(Status.Pass, "ValidateNotCategorisedFilterOption_SearchFunctionalityUS213AC passed");
    }

    [Test, Order(5)]
    public async Task ValidateNotCategorisedFilterOptions_CombinedWithKeywordSearchFunctionalityUS213AC()
    {
        await productsSearchPage.SearchByKeywordAsync("Apprentice");
        await filterPanel.OpenPhaseAsync();
        await filterPanel.CheckFilterAsync(filterPanel.Phase_NotCategorised);
        await filterPanel.ApplyFiltersAsync();
        // "1 result" or "N results": how many products carry the keyword is the data's business, the panel's wording is the page's.
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("for your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Search_FilterHeading, "Search term");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.KeywordSearchTag, "Apprentice × Remove Apprentice filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Phase_FilterHeading, "Phase");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Phase_NotCategorised, "Not categorised × Remove Not categorised filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await filterPanel.ClearAllFiltersAsync();

        ExtentTest?.Log(Status.Pass, "ValidateNotCategorisedFilterOptions_CombinedWithKeywordSearchFunctionalityUS213AC passed");
    }
}
