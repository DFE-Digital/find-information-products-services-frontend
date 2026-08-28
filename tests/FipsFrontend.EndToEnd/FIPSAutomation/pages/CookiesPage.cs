using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class CookiesPage
    {
        private readonly IPage page;

        private ILocator AnalyticsOff => page.Locator("#analytics-off");
        private ILocator AnalyticsOn => page.Locator("#analytics-on");
        private ILocator SuccessAlert => page.Locator(".govuk-notification-banner.govuk-notification-banner--success");
        private ILocator BackToHomeButton => page.Locator("//a[@class='govuk-button govuk-button--secondary' and contains (text(), 'Back to home')]");
        public CookiesPage(IPage page)
        {
            this.page = page;
        }

        public async Task VerifyCookiePreferencesVisibleAsync()
        {
            await Assertions.Expect(page.GetByText("Cookie preferences", new() { Exact = true })).ToBeVisibleAsync();
        }

        public async Task VerifySaveButtonVisibleAsync()
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { NameString = "Save cookie preferences" })).ToBeVisibleAsync();
        }

        public async Task VerifyCancelLinkVisibleAsync()
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { NameString = "Cancel" })).ToBeVisibleAsync();
        }

        public async Task VerifyChangeCookiePreferencesLinkAsync()
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { NameString = "Change cookie preferences" })).ToBeVisibleAsync();
        }

        public async Task VerifyBackToHomeLinkAsync()
        {
            await Assertions.Expect(BackToHomeButton).ToBeVisibleAsync();
        }

        public async Task SelectAnalyticsOffAsync()
        {
            await AnalyticsOff.CheckAsync();
        }

        public async Task SelectAnalyticsOnAsync()
        {
            await AnalyticsOn.CheckAsync();
        }

        public async Task SaveCookiePreferencesAsync()
        {
            await page.GetByRole(AriaRole.Button, new() { NameString = "Save cookie preferences" }).ClickAsync();
        }

        public async Task VerifySuccessBannerAsync()
        {
            bool alert = await SuccessAlert.IsVisibleAsync();
            Assert.That(alert, Is.True);
            await Assertions.Expect(SuccessAlert).ToContainTextAsync("Your cookie preferences have been saved.");
        }

        public async Task ClickCancelAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Cancel" }).ClickAsync();
        }

        public async Task ClickBackToHomeAsync()
        {
            await BackToHomeButton.ClickAsync();
        }
    }
}
