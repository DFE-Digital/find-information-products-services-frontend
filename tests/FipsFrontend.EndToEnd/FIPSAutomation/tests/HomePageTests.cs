using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(1)]
[Category("Functional")]
public class HomePageTests : BaseTest
{
    private HomePage homePage = null!;
    private RequestNewProductPage requestNewProductPage = null!;
    private HeaderComponent header = null!;

    [OneTimeSetUp] 
    public void InitPages()
    {
        homePage = new HomePage(Page);
        requestNewProductPage = new RequestNewProductPage(Page);
        header = new HeaderComponent(Page);
    }

    [Test, Order(1)]
    [Description("Login using username/password")]
    public async Task LoginWithUsernameAndPasswordUS231AC2()
    {
        // Login handled by GlobalSetup
        ExtentTest?.Log(Status.Pass, "LoginWithUsernameAndPasswordUS231AC2 passed");
    }

    [Test, Order(2)]
    public async Task HomePageVerificationUS12AC1()
    {
        await homePage.VerifyPageTitleAsync();
        await homePage.VerifyMainHeadingAsync();
        await homePage.VerifyServiceDescriptionAsync();
        await homePage.VerifySearchButtonTextAsync();
        ExtentTest?.Log(Status.Pass, "HomePageVerificationUS12AC1 assertion passed");
    }

    [Test, Order(3)]
    public async Task ClickMainSearchButtonUS12AC2()
    {
        await homePage.ClickSearchButtonAsync();
        await Assertions.Expect(Page.GetByText("Search and filter products and services")).ToBeVisibleAsync();
        await Page.GoBackAsync();
        ExtentTest?.Log(Status.Pass, "ClickSearchButtonUS12AC2 passed");
    }

    [Test, Order(4)]
    public async Task VerifyHomePageChangesUS305AC1()
    {
        await homePage.VerifyPageHeadingsAsync();
        await homePage.VerifySearchProductsAndServicesButtonAsync();
        await homePage.ClickSearchProductsAndServicesButtonAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Search and filter products and services" })).ToBeVisibleAsync();
        await Page.GoBackAsync();
        ExtentTest?.Log(Status.Pass, "VerifyHomePageUpdatesUS305AC1 passed");
    }

    [Test, Order(5)]
    public async Task VerifySearchLinkFunctionalityUS305AC2()
    {
        await homePage.VerifySearchLinkDescription();
        await homePage.ClickSearchLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Search and filter products and services" })).ToBeVisibleAsync();
        await Page.GoBackAsync();
        ExtentTest?.Log(Status.Pass, "VerifySearchLinkFunctionalityUS305AC2 passed");
    }

    [Test, Order(6)]
    public async Task VerifyRequestANewProductEntryLinkUS305AC3()
    {
        await homePage.ClickRequestNewProductEntryLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Request a new product entry" })).ToBeVisibleAsync();
        await requestNewProductPage.VerifyFormVisibleAsync();
        await header.ClickServiceNameLinkAsync();
        ExtentTest?.Log(Status.Pass, "VerifyRequestANewProductEntryLinkUS305AC3 passed");
    }

    [Test, Order(7)]
    public async Task VerifyContactLinkInFooterUS305AC4()
    {
        await homePage.ClickContactLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Contact us" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(homePage.ServiceEmailDesc)).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(homePage.EmailLink)).ToHaveAttributeAsync("href", "mailto:fips.service@education.gov.uk");
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Give feedback" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("If you have any feedback for the service, use the feedback link at the end of each page.")).ToBeVisibleAsync();
        await homePage.ClickBackToHomeAsync();
        await homePage.VerifyMainHeadingAsync();
        ExtentTest?.Log(Status.Pass, "VerifyContactLinkInFooterUS305AC4 passed");
    }
}
