using System.Text.RegularExpressions;
using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(19)]
[Category("Functional")]
public class FeedbackAndSurveyTests : BaseTest
{
    private ProductDetailPage productDetailPage = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productDetailPage = new ProductDetailPage(Page);
    }

    [Test, Order(1)]
    public async Task VerifyFeedbackLinks_ContentChangeUS207AC()
    {
        await NavigateToAsync("");
        await productDetailPage.VerifyFeedbackBannerAsync();
        var survey = Page.Locator("//*[@id=\"feedback-link-text\"]/a[1]");
        await Assertions.Expect(survey).ToContainTextAsync("Give us feedback about this service");
        // #207 AC2 names a survey address. The address is the instance's Feedback:SurveyUrl setting and the survey is
        // the research platform's page, so an absolute link is asserted, not the platform's title or question.
        await Assertions.Expect(survey).ToHaveAttributeAsync("href", new Regex("^https?://"));
        ExtentTest?.Log(Status.Pass, "VerifyFeedbackLinks_ContentChangeUS207AC passed");
    }

    [Test, Order(2)]
    public async Task VerifyFeedbackSurveyLinkStaysInTheSameTabUS226()
    {
        // #207 AC2 asked for the survey to open in a new window. #226 (accessibility: a new tab strands keyboard and
        // screen-reader users) removed every new-tab opening, the survey link among them, so the link stays in the
        // same tab and the survey itself is not opened here.
        await NavigateToAsync("");
        var survey = Page.Locator("//*[@id=\"feedback-link-text\"]/a[1]");
        await Assertions.Expect(survey).ToContainTextAsync("Give us feedback about this service");
        await Assertions.Expect(survey).Not.ToHaveAttributeAsync("target", new Regex(".*"));
        await Assertions.Expect(survey).ToHaveAttributeAsync("href", new Regex("^https?://"));
        ExtentTest?.Log(Status.Pass, "VerifyFeedbackSurveyLinkStaysInTheSameTabUS226 passed");
    }
}
