using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(9)]
[Category("Functional")]
public class CookiesAndFooterTests : BaseTest
{
    private CookiesPage cookiesPage = null!;
    private FooterComponent footer = null!;
    private AccessibilityStatementPage accessibilityPage = null!;
    private HeaderComponent header = null!;
    private HomePage homePage = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        cookiesPage = new CookiesPage(Page);
        footer = new FooterComponent(Page);
        accessibilityPage = new AccessibilityStatementPage(Page);
        header = new HeaderComponent(Page);
        homePage = new HomePage(Page);
    }

    [Test, Order(1)]
    public async Task VerifyFooterCookiesLinkUS13AC()
    {
        await footer.ClickCookiesLinkAsync();
        await cookiesPage.VerifyCookiePreferencesVisibleAsync();
        await cookiesPage.VerifySaveButtonVisibleAsync();
        await cookiesPage.VerifyCancelLinkVisibleAsync();
        await cookiesPage.VerifyChangeCookiePreferencesLinkAsync();
        await cookiesPage.VerifyBackToHomeLinkAsync();
        ExtentTest?.Log(Status.Pass, "VerifyFooterCookiesLinkUS13AC passed");
    }

    [Test, Order(2)]       
    public async Task VerifyCookiesPageFunctionalitiesUS13AC()
    {
        await NavigateToAsync("cookies");
        await cookiesPage.SelectAnalyticsOffAsync();
        await cookiesPage.SaveCookiePreferencesAsync();
        await cookiesPage.VerifySuccessBannerAsync();
        await cookiesPage.SelectAnalyticsOnAsync();
        await cookiesPage.ClickCancelAsync();
        await homePage.VerifyMainHeadingAsync();
        ExtentTest?.Log(Status.Pass, "VerifyCookiesPageFunctionalitiesUS13AC passed");
    }

    [Test, Order(3)]
    public async Task VerifyCookiesPageBackToHomeFunctionalityUS13AC()
    {
        await NavigateToAsync("cookies");
        await cookiesPage.ClickBackToHomeAsync();
        await homePage.VerifyMainHeadingAsync();
        ExtentTest?.Log(Status.Pass, "VerifyCookiesPageBackToHomeFunctionalityUS13AC passed");
    }

    [Test, Order(4)]
    public async Task VerifyAccessibilityLinkUS16AC()
    {
        await footer.ClickAccessibilityStatementAsync();
        await accessibilityPage.VerifyAccessibilityStatementVisibleAsync();
        await header.ClickServiceNameLinkAsync();
        await homePage.VerifyMainHeadingAsync();
        ExtentTest?.Log(Status.Pass, "VerifyAccessibilityLinkUS16AC passed");
    }

    [Test, Order(5)]
    public async Task VerifyPrivacyPolicyLinkUS15AC()
    {
        //Fails because Privacy link is coded to open in a new tab, need to update after March 2026 release
        var newTab = await Page.RunAndWaitForPopupAsync(async () =>
        {
            await Page.GetByRole(AriaRole.Link, new() { NameString = "Privacy policy" }).ClickAsync();
        });

        await newTab.WaitForLoadStateAsync();
        await newTab.GetByRole(AriaRole.Button, new() { NameString = "Accept additional cookies" }).ClickAsync();
        await newTab.GetByRole(AriaRole.Button, new() { NameString = "Hide cookie message" }).ClickAsync();

        // Assertions in the new tab
        await Assertions.Expect(newTab).ToHaveURLAsync(ProductDetailPage.GOV_URL);        
        await Assertions.Expect(newTab).ToHaveTitleAsync("Personal information charter - Department for Education - GOV.UK");          
        await Assertions.Expect(newTab.GetByRole(AriaRole.Heading, new() { NameString = "Personal information charter" })).ToBeVisibleAsync();
        await newTab.CloseAsync();
        // Assertions back on the original page
        await Assertions.Expect(Page).ToHaveTitleAsync("Find information about products and services - FIPS");

        ExtentTest?.Log(Status.Pass, "VerifyPrivacyPolicyLinkUS15AC passed");
    }
}
