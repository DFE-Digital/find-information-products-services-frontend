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
        await Assertions.Expect(Page.Locator("//*[@id=\"feedback-link-text\"]/a[1]")).ToContainTextAsync("Give us feedback about this service");
        var newTab = await Page.RunAndWaitForPopupAsync(async () =>
        {
            await productDetailPage.ClickSurveyLinkAsync();
        });
        await newTab.WaitForLoadStateAsync();

        // Assertions in the new tab
        await Assertions.Expect(newTab).ToHaveURLAsync("https://dferesearch.fra1.qualtrics.com/jfe/form/SV_bHoLXsj3BfAh3ZI");
        await Assertions.Expect(newTab).ToHaveTitleAsync("Qualtrics Survey | Qualtrics Experience Management");
        await Assertions.Expect(newTab.Locator("//div[@class='QuestionText BorderColor']")).ToContainTextAsync("Thank you for taking the time to give feedback on your experience of using ‘Find Information about Products and Services’ (FIPS) today.   This survey should take no more than 2 minutes to complete, and your contribution will help us to: - Understand your needs with regards to the FIPS service - Make future improvements to FIPS");
        await newTab.CloseAsync();

        // Assertions back on the original page
        await Assertions.Expect(Page).ToHaveTitleAsync("Find information about products and services - FIPS");

        ExtentTest?.Log(Status.Pass, "VerifyFeedbackLinks_ContentChangeUS207AC passed");
    }
}
