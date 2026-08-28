using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(10)]
[Category("Functional")]
public class FeedbackTests : BaseTest
{
    private FeedbackSectionComponent feedback = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        feedback = new FeedbackSectionComponent(Page);
    }

    [Test, Order(1)]
    public async Task VerifyGiveFeedbackOrReportAProblemWithThisPageLinkUS162AC()
    {
        await NavigateToAsync("");
        string feedbackText = "Verifying feedback form - User can enter their feedback or report a problem with this page and submit the form successfully.";
        await feedback.OpenFeedbackFormAsync();
        await feedback.VerifyFeedbackFormVisibleAsync();
        await feedback.VerifyCharacterLimitHintAsync();
        await feedback.FillFeedbackAsync(feedbackText);
        await feedback.SubmitFeedbackAsync();
        await Task.Delay(2000);
        await feedback.VerifySuccessMessageAsync();
        ExtentTest?.Log(Status.Pass, "VerifyGiveFeedbackOrReportAProblemWithThisPageLinkUS162AC passed");
    }

    [Test, Order(2)]
    public async Task ValidateMaxCharsinGiveFeedbackOrReportAProblemFormUS162AC()
    {
        await NavigateToAsync("");
        string feedbackText = "Validating more than 1000 characters for feedback form. User should not be able to submit their feedback after entering more than limited characters in given textarea. abc defghi jklm nop qrst uvw xyz012 34567Validating 1000 characters for feedback form. User should be able to enter their feedback or report a problem with this page and submit the form successfully. abc defghi jklm nop qrst uvw xyz012 34567Validating 1000 characters for feedback form. User should be able to enter their feedback or report a problem with this page and submit the form successfully. abc defghi jklm nop qrst uvw xyz012 34567Validating 1000 characters for feedback form. User should be able to enter their feedback or report a problem with this page and submit the form successfully. abc defghi jklm nop qrst uvw xyz012 34567Validating 1000 characters for feedback form. User should be able to enter their feedback or report a problem with this page and submit the form successfully. abc defghi jklm nop qrst uvw xyz. Test";
        await feedback.OpenFeedbackFormAsync();
        await feedback.VerifyFeedbackFormVisibleAsync();
        await feedback.FillFeedbackAsync(feedbackText);
        await feedback.VerifyMaxCharsErrorAsync("You have 6 characters too many");
        await feedback.SubmitFeedbackAsync();
        await Task.Delay(1000);
        await feedback.VerifySubmitErrorAsync();
        await feedback.VerifyErrorLinkVisibleAsync();
        ExtentTest?.Log(Status.Pass, "ValidateMaxCharsinGiveFeedbackOrReportAProblemFormUS162AC passed");
    }
}
