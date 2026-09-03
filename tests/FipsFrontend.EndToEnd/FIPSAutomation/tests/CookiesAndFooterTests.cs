using System.Text.RegularExpressions;
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
        // #16 AC1: a link at the bottom of every page; AC2: it leads to the service's accessibility statement. Where the
        // statement lives is the instance's AccessibilityStatement:Url setting and the statement is another service's
        // page, so an absolute link is asserted, not a particular address and not the statement's content.
        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Accessibility statement", Exact = true});
        await Assertions.Expect(link).ToBeVisibleAsync();
        await Assertions.Expect(link).ToHaveAttributeAsync("href", new Regex("^https?://"));
        ExtentTest?.Log(Status.Pass, "VerifyAccessibilityLinkUS16AC passed");
    }

    [Test, Order(5)]
    public async Task VerifyPrivacyPolicyLinkUS15AC()
    {
        // #15 AC1: a link at the bottom of every page; AC2: it leads to the department's personal information charter
        // on GOV.UK. That page, its cookie banner and its heading are GOV.UK's, so they are not asserted here.
        var link = Page.GetByRole(AriaRole.Link, new() { Name = "Privacy policy", Exact = true});
        await Assertions.Expect(link).ToBeVisibleAsync();
        await Assertions.Expect(link).ToHaveAttributeAsync("href", ProductDetailPage.GOV_URL);

        ExtentTest?.Log(Status.Pass, "VerifyPrivacyPolicyLinkUS15AC passed");
    }
}
