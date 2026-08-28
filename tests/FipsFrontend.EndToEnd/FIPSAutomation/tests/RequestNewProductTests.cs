using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(20)]
[Category("Functional")]
public class RequestNewProductTests : BaseTest
{
    private ProductsSearchPage productsSearchPage = null!;
    private RequestNewProductPage requestNewProductPage = null!;
    private FilterPanelComponent filterPanel = null!;

    public static readonly string TEST_URL = "https://automation-test-product.education.gov.uk/";

    [OneTimeSetUp]
    public void InitPages()
    {
        productsSearchPage = new ProductsSearchPage(Page);
        requestNewProductPage = new RequestNewProductPage(Page);
        filterPanel = new FilterPanelComponent(Page);
    }

    [Test, Order(1)]
    [Ignore("This test triggers product entry request email to FIPS Inbox. So, skipped for now")]
    public async Task VerifyRequestNewProductEntryForm_AddingProductUS141AC()
    {
        await NavigateToAsync("products");
        await productsSearchPage.ClickMissingProductLinkAsync();
        await productsSearchPage.ClickRequestNewProductEntryLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Request a new product entry" })).ToBeVisibleAsync();
        await requestNewProductPage.VerifyFormFieldsAsync();
        await Assertions.Expect(Page.GetByText("Use this form to request a new product to be added to FIPS. An administrator will review your request before it is added.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("This form should only be submitted by a permanent member of DfE staff.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Product details" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByLabel("Product title")).ToBeVisibleAsync();
        await requestNewProductPage.FillProductTitleAsync("Automation - Test Product Title");
        await Assertions.Expect(Page.GetByLabel("Description")).ToBeVisibleAsync();
        await requestNewProductPage.FillProductDescriptionAsync("Automation Test - Adding description of more than 1000 characters - Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE - Test description ends.");
        await Assertions.Expect(Page.GetByLabel("Product URL (optional)")).ToBeVisibleAsync();
        await requestNewProductPage.FillProductUrlAsync(TEST_URL);
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Product classification" })).ToBeVisibleAsync();
        await requestNewProductPage.CheckPhaseAsync(1);
        await requestNewProductPage.CheckBusinessAreaAsync(1);
        await requestNewProductPage.CheckAllChannelsAsync();
        await requestNewProductPage.CheckAllTypesAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Additional information" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByLabel("Tell us in your own words who the users of the product are (optional)")).ToBeVisibleAsync();
        await requestNewProductPage.FillAdditionalInfoAsync("Additional information - adding automation test case for new product entry form. This product can be used by teachers, school leaders and responsible bodies.");
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Team roles (optional)" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByLabel("Delivery Manager")).ToBeVisibleAsync();
        await requestNewProductPage.FillDeliveryManagerAsync("Automation test Delivery Manager");
        await Assertions.Expect(Page.GetByLabel("Product Manager")).ToBeVisibleAsync();
        await requestNewProductPage.FillProductManagerAsync("Automation test Product Manager");
        await Assertions.Expect(Page.GetByLabel("Senior Responsible Officer")).ToBeVisibleAsync();
        await requestNewProductPage.FillSeniorRespOfficerAsync("Automation test Senior Resp Officer");
        await Assertions.Expect(Page.GetByLabel("Notes")).ToBeVisibleAsync();
        await requestNewProductPage.VerifyNotesHintAsync("Provide any additional information that would help the FIPS team understand this request.");
        await requestNewProductPage.FillNotesAsync("Automation test - providing additional information that would help the FIPS team understand this request.");
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Cancel" })).ToBeVisibleAsync();
        await Task.Delay(2000);
        await requestNewProductPage.SubmitBlankFormAsync(); // reuses SubmitRequestButton click
        await Task.Delay(1000);
        await requestNewProductPage.VerifySuccessAlertAsync();
        await requestNewProductPage.VerifySuccessAlertTextAsync("Your new product entry request has been submitted. The FIPS team will review your request and may contact you if additional information is needed.");

        ExtentTest?.Log(Status.Pass, "VerifyRequestNewProductEntryForm_AddingProductUS141AC passed");
    }

    [Test, Order(2)]
    [Ignore("This test triggers product entry request email to FIPS Inbox. So, skipped for now")]
    public async Task ValidateRequestNewProductForm_SubmitBlankFormUS141AC()
    {
        await requestNewProductPage.SubmitBlankFormAsync();
        await requestNewProductPage.VerifyValidationErrorsAsync();
        await requestNewProductPage.VerifyErrorMessageBoxAsync("There is a problem");
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Please provide a product title" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Please provide a description" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Please provide notes about this request" })).ToBeVisibleAsync();

        // Clicking on error messages links
        await requestNewProductPage.ClickTitleErrorLinkAsync();
        await requestNewProductPage.VerifyTitleFocusedAsync();
        await requestNewProductPage.FillProductTitleAsync("Automation test - Product Title A");
        await requestNewProductPage.VerifyTitleErrorAsync("Error:\r\n                        Please provide a product title");

        await requestNewProductPage.ClickDescriptionErrorLinkAsync();
        await requestNewProductPage.VerifyDescriptionFocusedAsync();
        await requestNewProductPage.FillProductDescriptionAsync("Automation test - Submitted blank form and then verifying all error messages for mandatory fields.");
        await requestNewProductPage.VerifyDescriptionErrorAsync("Error:\r\n                        Please provide a description");

        await requestNewProductPage.ClickNotesErrorLinkAsync();
        await requestNewProductPage.VerifyNotesFocusedAsync();
        await requestNewProductPage.FillNotesAsync("Automation test -  Submitting form after entering only mandatory details");
        await requestNewProductPage.VerifyNotesErrorAsync("Error:\r\n                        Please provide notes about this request");
        await Task.Delay(2000);
        await requestNewProductPage.SubmitBlankFormAsync();
        await Task.Delay(1000);
        await requestNewProductPage.VerifySuccessAlertAsync();
        await requestNewProductPage.VerifySuccessAlertTextAsync("Your new product entry request has been submitted. The FIPS team will review your request and may contact you if additional information is needed.");

        ExtentTest?.Log(Status.Pass, "ValidateRequestNewProductForm_SubmitBlankFormUS141AC passed");
    }

    [Test, Order(3)]
    public async Task VerifyRequestNewProductForm_AddDetailsAndClickCancelUS141AC()
    {
        await NavigateToAsync("products");
        await productsSearchPage.ClickMissingProductLinkAsync();
        await productsSearchPage.ClickRequestNewProductEntryLinkAsync();
        await requestNewProductPage.VerifyFormVisibleAsync();
        await requestNewProductPage.FillProductTitleAsync("Automation test - Product Title B");
        await requestNewProductPage.FillProductDescriptionAsync("Automation test - Adding description for new product");
        await requestNewProductPage.CheckPhaseAsync(2);
        await requestNewProductPage.CheckBusinessAreaAsync(2);
        await requestNewProductPage.CheckChannelAsync(5);
        await requestNewProductPage.FillAdditionalInfoAsync("Additional information - validating Cancel button functionality");
        await requestNewProductPage.FillDeliveryManagerAsync("Automation - DfE Delivery Manager");
        await requestNewProductPage.FillNotesAsync("Automation test - adding all necessary details and then clicking on Cancel");
        await requestNewProductPage.CancelFormAsync();
        await Assertions.Expect(Page).ToHaveTitleAsync("Search and filter products and services - FIPS");
        await productsSearchPage.VerifyProductsPageHeadingAsync();

        ExtentTest?.Log(Status.Pass, "VerifyRequestNewProductForm_AddDetailsAndClickCancelUS141AC passed");
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
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("results for your selected filters");
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
