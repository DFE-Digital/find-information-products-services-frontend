using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(16)]
[Category("Functional")]
public class FilterCombinationTests : BaseTest
{
    private ProductsSearchPage productsSearchPage = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productsSearchPage = new ProductsSearchPage(Page);
    }

    [Test, Order(1)]
    public async Task ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC1()
    {
        await NavigateToAsync("products");
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_SchoolsDigital);
        await productsSearchPage.FilterPanel.OpenChannelAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Channel_Web);
        await productsSearchPage.FilterPanel.OpenPhaseAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Phase_PublicBeta);
        await productsSearchPage.FilterPanel.OpenTypeAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Type_API);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Phase_FilterHeading, "Phase");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Phase_PublicBeta, "Public beta × Remove Public beta filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Channel_FilterHeading, "Channel");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Channel_Web, "Web × Remove Web filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_API, "API × Remove API filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_SchoolsDigital, "Schools Digital × Remove Schools Digital filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC1 passed");
    }

    [Test, Order(2)]
    public async Task ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC2()
    {
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_OperationsAndInfra);
        await productsSearchPage.FilterPanel.OpenPhaseAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Phase_Live);
        await productsSearchPage.FilterPanel.OpenTypeAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Type_Transactional);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await Task.Delay(1000);
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Phase_FilterHeading, "Phase");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Phase_Live, "Live × Remove Live filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_Transactional, "Transactional × Remove Transactional filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_OperationsAndInfra, "Operations and Infrastructure × Remove Operations and Infrastructure filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await Task.Delay(1000);
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC2 passed");
    }

    [Test, Order(3)]
    public async Task ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC3()
    {
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_SkillsAndGrowth);
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_Strategy);
        await productsSearchPage.FilterPanel.OpenPhaseAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Phase_Decommissioned);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Phase_FilterHeading, "Phase");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Phase_Decommissioned, "Decommissioned × Remove Decommissioned filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_SkillsAndGrowth, "Skills and Growth × Remove Skills and Growth filter");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_Strategy, "Strategy × Remove Strategy filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC3 passed");
    }

    [Test, Order(4)]
    public async Task ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC4()
    {
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_ChildrenAndFamilies);
        await productsSearchPage.FilterPanel.OpenChannelAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Channel_FaceToFace);
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Channel_Web);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Channel_FilterHeading, "Channel");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Channel_FaceToFace, "Face-to-face × Remove Face-to-face filter");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Channel_Web, "Web × Remove Web filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_ChildrenAndFamilies, "Children and Families × Remove Children and Families filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC4 passed");
    }

    [Test, Order(5)]
    public async Task ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC5()
    {
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_FundingAndFinancialOversight);
        await productsSearchPage.FilterPanel.OpenPhaseAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Phase_Live);
        await productsSearchPage.FilterPanel.OpenTypeAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Type_Information);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Phase_FilterHeading, "Phase");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Phase_Live, "Live × Remove Live filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_Information, "Information × Remove Information filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_FundingAndFinancialOversight, "Funding and Financial Oversight × Remove Funding and Financial Oversight filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC5 passed");
    }

    [Test, Order(6)]
    public async Task ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC6()
    {
        await productsSearchPage.FilterPanel.OpenBusinessAreaAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.BA_EnterpriseData);
        await productsSearchPage.FilterPanel.OpenPhaseAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Phase_NotCategorised);
        await productsSearchPage.FilterPanel.OpenTypeAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Type_Data);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync(" your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Phase_FilterHeading, "Phase");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Phase_NotCategorised, "Not categorised × Remove Not categorised filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Type_FilterHeading, "Type");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_Data, "Data    × Remove Data    filter");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.BA_FilterHeading, "Business area");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_EnterpriseData, "Enterprise Data × Remove Enterprise Data filter");
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC6 passed");
    }

    [Test, Order(7)]
    public async Task VerifyMissingProductOrServiceLink_ProductsPageUS219AC()
    {
        await NavigateToAsync("products");
        await productsSearchPage.ClickMissingProductLinkAsync();
        await productsSearchPage.VerifyMissingProductSectionVisibleAsync();
        await productsSearchPage.VerifyRequestNewProductDescAsync("If you cannot find a product or service, make a ");
        await productsSearchPage.ClickRequestNewProductEntryLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Request a new product entry" })).ToBeVisibleAsync();
        await Page.GoBackAsync();
        ExtentTest?.Log(Status.Pass, "VerifyMissingProductOrServiceLink_ProductsPageUS219AC");
    }
}
