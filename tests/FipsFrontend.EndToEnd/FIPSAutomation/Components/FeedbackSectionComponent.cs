using Microsoft.Playwright;

namespace FiPSAutomation.Components
{
    public class FeedbackSectionComponent
    {
        private readonly IPage page;

        private ILocator FeedbackTextbox => page.Locator("textarea#feedback_form_input");
        private ILocator FeedbackFormErrorMsg => page.Locator("#feedback_form_input-error");
        private ILocator FeedbackMaxCharsErrorMsg => page.Locator("//*[@id=\"feedback_form_group\"]/div[3]");
        private ILocator FeedbackSubmitErrorMsg => page.Locator("//div[@id=\"feedback-error-summary\"]");

        public FeedbackSectionComponent(IPage page)
        {
            this.page = page;
        }

        public async Task OpenFeedbackFormAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "tell us about a problem with this page" }).ClickAsync();
        }

        public async Task VerifyFeedbackFormVisibleAsync()
        {
            await Assertions.Expect(page.GetByText("What do you want to tell us?", new() { Exact = true })).ToBeVisibleAsync();
        }

        public async Task VerifyCharacterLimitHintAsync()
        {
            await Assertions.Expect(FeedbackFormErrorMsg).ToContainTextAsync("Your feedback must be 1000 characters or fewer");
        }

        public async Task FillFeedbackAsync(string text)
        {
            await FeedbackTextbox.FillAsync(text);
        }

        public async Task SubmitFeedbackAsync()
        {
            await page.GetByRole(AriaRole.Button, new() { NameString = "Submit feedback" }).ClickAsync();
        }

        public async Task VerifySuccessMessageAsync()
        {
            await Assertions.Expect(page.GetByText("Thank you for your feedback", new() { Exact = true })).ToBeVisibleAsync();
        }

        public async Task VerifyMaxCharsErrorAsync(string expectedText)
        {
            await Assertions.Expect(FeedbackMaxCharsErrorMsg).ToHaveTextAsync(expectedText);
        }

        public async Task VerifySubmitErrorAsync()
        {
            await Assertions.Expect(FeedbackSubmitErrorMsg).ToContainTextAsync("There is a problem");
        }

        public async Task VerifyErrorLinkVisibleAsync()
        {
            await Assertions.Expect(
                page.GetByRole(AriaRole.Link, new() { NameString = "Your feedback must be 1000 characters or fewer" })
            ).ToBeVisibleAsync();
        }
    }
}
