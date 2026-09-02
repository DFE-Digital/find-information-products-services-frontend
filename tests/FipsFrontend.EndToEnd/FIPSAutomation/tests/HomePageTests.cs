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
    private HeaderComponent header = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        homePage = new HomePage(Page);
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

    // #305 AC2 and AC3 (the "Search" and "request a new product entry" links in the "Update a product or
    // service" section) no longer apply since #315 (part of #308): the section says to contact the team.

    [Test, Order(7)]
    public async Task VerifyContactLinkInFooterUS305AC4()
    {
        await homePage.ClickContactLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Contact us" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(homePage.ServiceEmailDesc)).ToBeVisibleAsync();
        // #305 AC4: the page displays how to contact the team. The address is the instance's Contact:Email setting, so a
        // mailto link is asserted, not a particular mailbox.
        await Assertions.Expect(Page.Locator(homePage.EmailLink)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Give feedback" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("If you have any feedback for the service, use the feedback link at the end of each page.")).ToBeVisibleAsync();
        await homePage.ClickBackToHomeAsync();
        await homePage.VerifyMainHeadingAsync();
        ExtentTest?.Log(Status.Pass, "VerifyContactLinkInFooterUS305AC4 passed");
    }
}
