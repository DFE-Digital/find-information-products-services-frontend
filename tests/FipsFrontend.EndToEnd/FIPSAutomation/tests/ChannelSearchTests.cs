using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;

namespace FiPSAutomation;

[TestFixture, Order(13)]
[Category("Functional")]
public class ChannelSearchTests : BaseTest
{
    private ProductsSearchPage productsSearchPage = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productsSearchPage = new ProductsSearchPage(Page);
    }

    private async Task VerifyChannelFilterAsync(string checkboxLocator, string filterTagLocator, string expectedTagText, bool hasPagination = false)
    {
        await productsSearchPage.FilterPanel.VerifyChannelVisibleAsync();
        await productsSearchPage.FilterPanel.OpenChannelAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(checkboxLocator);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await Task.Delay(1000);
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Channel_FilterHeading, "Channel");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(filterTagLocator, expectedTagText);
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
    }

    [Test, Order(1)]
    public async Task VerifyChannelSearchFunctionality_ChatCategoryUS235AC1()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_Chat, productsSearchPage.FilterTags.Channel_Chat, "Chat × Remove Chat filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_ChatCategoryUS235AC1 passed");
    }

    [Test, Order(2)]
    public async Task VerifyChannelSearchFunctionality_EmailCategoryUS235AC2()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_Email, productsSearchPage.FilterTags.Channel_Email, "Email × Remove Email filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_EmailCategoryUS235AC2 passed");
    }

    [Test, Order(3)]
    public async Task VerifyChannelSearchFunctionality_FaceToFaceCategoryUS235AC3()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_FaceToFace, productsSearchPage.FilterTags.Channel_FaceToFace, "Face-to-face × Remove Face-to-face filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_FaceToFaceCategoryUS235AC3 passed");
    }

    [Test, Order(4)]
    public async Task VerifyChannelSearchFunctionality_NativeAppCategoryUS235AC4()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_NativeApp, productsSearchPage.FilterTags.Channel_NativeApp, "Native app × Remove Native app filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_NativeAppCategoryUS235AC4 passed");
    }

    [Test, Order(5)]
    public async Task VerifyChannelSearchFunctionality_OtherDigitalMediaCategoryUS235AC5()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_OtherDigitalMedia, productsSearchPage.FilterTags.Channel_OtherDigitalMedia, "Other digital media × Remove Other digital media filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_OtherDigitalMediaCategoryUS235AC5 passed");
    }

    [Test, Order(6)]
    public async Task VerifyChannelSearchFunctionality_PhoneCategoryUS235AC6()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_Phone, productsSearchPage.FilterTags.Channel_Phone, "Phone × Remove Phone filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_PhoneCategoryUS235AC6 passed");
    }

    [Test, Order(7)]
    public async Task VerifyChannelSearchFunctionality_PrintMediaCategoryUS235AC7()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_PrintMedia, productsSearchPage.FilterTags.Channel_PrintMedia, "Print media × Remove Print media filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_PrintMediaCategoryUS235AC7 passed");
    }

    [Test, Order(8)]
    public async Task VerifyChannelSearchFunctionality_SMSCategoryUS235AC8()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_SMS, productsSearchPage.FilterTags.Channel_SMS, "SMS × Remove SMS filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_SMSCategoryUS235AC8 passed");
    }

    [Test, Order(9)]
    public async Task VerifyChannelSearchFunctionality_WebCategoryUS235AC9()
    {
        await productsSearchPage.FilterPanel.VerifyChannelVisibleAsync();
        await productsSearchPage.FilterPanel.OpenChannelAsync();
        await productsSearchPage.FilterPanel.CheckFilterAsync(productsSearchPage.FilterPanel.Channel_Web);
        await productsSearchPage.FilterPanel.ApplyFiltersAsync();
        await Task.Delay(1000);
        await productsSearchPage.FilterTags.VerifyAppliedFiltersPanelContainsAsync("your selected filters");
        await productsSearchPage.FilterTags.VerifyFilterHeadingAsync(productsSearchPage.FilterTags.Channel_FilterHeading, "Channel");
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Channel_Web, "Web × Remove Web filter");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(2);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?channel=web&page=2");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToPageAsync(3);
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?channel=web&page=3");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await productsSearchPage.Pagination.GoToNextPageAsync();
        await productsSearchPage.Pagination.VerifyUrlContainsAsync("https://find-products-services-test.azurewebsites.net/Products?channel=web&page=4");
        await productsSearchPage.VerifyProductListVisibleAsync();
        await Task.Delay(1000);
        await productsSearchPage.FilterPanel.ClearAllFiltersAsync();
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_WebCategoryUS235AC9 passed");
    }

    [Test, Order(10)]
    public async Task VerifyChannelSearchFunctionality_NotCategorisedUS235AC10()
    {
        await VerifyChannelFilterAsync(productsSearchPage.FilterPanel.Channel_NotCategorised, productsSearchPage.FilterTags.Channel_NotCategorised, "Not categorised × Remove Not categorised filter");
        ExtentTest?.Log(Status.Pass, "VerifyChannelSearchFunctionality_NotCategorisedUS235AC10 passed");
    }
}
