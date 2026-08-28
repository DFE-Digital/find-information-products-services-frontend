using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class RequestNewProductPage
    {
        private readonly IPage page;

        private ILocator RequestForm => page.Locator("//form[@method='post']");
        private ILocator AddProductTitle => page.Locator("#Title");
        private ILocator AddProductDescription => page.Locator("#Description");
        private ILocator AddProductUrl => page.Locator("#ServiceUrl");
        private ILocator CheckPhaseText => page.Locator("legend:has-text('Phase')");
        private ILocator CheckBusinessAreaText => page.Locator("legend:has-text('Business area')");
        private ILocator CheckChannelsText => page.Locator("legend:has-text('Channels (optional)')");
        private ILocator CheckTypesText => page.Locator("legend:has-text('Types (optional)')");
        private ILocator AddProductPhase1 => page.Locator("#PhaseId_387");
        private ILocator AddProductPhase2 => page.Locator("#PhaseId_389");
        private ILocator AddProductBusinessArea1 => page.Locator("#BusinessAreaId_445");
        private ILocator AddProductBusinessArea2 => page.Locator("#BusinessAreaId_439");
        private ILocator AddProductChannelCB1 => page.Locator("#ChannelIds_380");
        private ILocator AddProductChannelCB2 => page.Locator("#ChannelIds_379");
        private ILocator AddProductChannelCB3 => page.Locator("#ChannelIds_377");
        private ILocator AddProductChannelCB4 => page.Locator("#ChannelIds_376");
        private ILocator AddProductChannelCB5 => page.Locator("#ChannelIds_382");
        private ILocator AddProductChannelCB6 => page.Locator("#ChannelIds_378");
        private ILocator AddProductChannelCB7 => page.Locator("#ChannelIds_381");
        private ILocator AddProductChannelCB8 => page.Locator("#ChannelIds_415");
        private ILocator AddProductChannelCB9 => page.Locator("#ChannelIds_375");
        private ILocator AddProductTypeCB1 => page.Locator("#TypeIds_416");
        private ILocator AddProductTypeCB2 => page.Locator("#TypeIds_394");
        private ILocator AddProductTypeCB3 => page.Locator("#TypeIds_453");
        private ILocator AddProductTypeCB4 => page.Locator("#TypeIds_393");
        private ILocator AddProductTypeCB5 => page.Locator("#TypeIds_392");
        private ILocator AddAdditionalInfo => page.Locator("#Users");
        private ILocator AddDeliveryManager => page.Locator("#DeliveryManager");
        private ILocator AddProductManager => page.Locator("#ProductManager");
        private ILocator AddSeniorRespOfficer => page.Locator("#SeniorResponsibleOfficer");
        private ILocator AddNotesTextbox => page.Locator("#Notes");
        private ILocator NotesHintText => page.Locator("#Notes-hint");
        private ILocator SubmitRequestButton => page.Locator("#submit-request-btn");
        private ILocator SuccessAlert => page.Locator("div[aria-labelledby = \"govuk-notification-banner-title\"]");
        private ILocator ErrorMessageBox => page.Locator("//div[@aria-labelledby='error-summary-title']");
        private ILocator TitleErrorMessage => page.Locator("(//span[@class='govuk-error-message'])[1]");
        private ILocator DescriptionErrorMessage => page.Locator("(//span[@class='govuk-error-message'])[2]");
        private ILocator NotesErrorMessage => page.Locator("(//span[@class='govuk-error-message'])[3]");

        public RequestNewProductPage(IPage page)
        {
            this.page = page;
        }

        public async Task VerifyFormFieldsAsync()
        {
            await Assertions.Expect(RequestForm).ToBeVisibleAsync();
            await Assertions.Expect(AddProductTitle).ToBeVisibleAsync();
            await Assertions.Expect(AddProductDescription).ToBeVisibleAsync();
            await Assertions.Expect(AddProductUrl).ToBeVisibleAsync();
            await Assertions.Expect(CheckPhaseText).ToBeVisibleAsync();
            await Assertions.Expect(CheckBusinessAreaText).ToBeVisibleAsync();
            await Assertions.Expect(CheckChannelsText).ToBeVisibleAsync();
            await Assertions.Expect(CheckTypesText).ToBeVisibleAsync();
            await Assertions.Expect(NotesHintText).ToBeVisibleAsync();
            await Assertions.Expect(SubmitRequestButton).ToBeVisibleAsync();
        }

        public async Task FillAndSubmitFormAsync()
        {
            await AddProductTitle.FillAsync("Test Product - Automation");
            await AddProductDescription.FillAsync("Test product description for automation testing");
            await AddProductUrl.FillAsync("https://test-product.example.com");
            await AddProductPhase1.CheckAsync();
            await AddProductBusinessArea1.CheckAsync();
            await AddProductChannelCB1.CheckAsync();
            await AddProductTypeCB1.CheckAsync();
            await AddNotesTextbox.FillAsync("Automation test note");
            await SubmitRequestButton.ClickAsync();
        }

        public async Task FillAndCancelFormAsync()
        {
            await AddProductTitle.FillAsync("Test Product - Cancel");
            await AddProductDescription.FillAsync("Cancel test description");
            await page.GetByRole(AriaRole.Link, new() { NameString = "Cancel" }).ClickAsync();
        }

        public async Task SubmitBlankFormAsync()
        {
            await SubmitRequestButton.ClickAsync();
        }

        public async Task VerifyValidationErrorsAsync()
        {
            await Assertions.Expect(ErrorMessageBox).ToBeVisibleAsync();
            await Assertions.Expect(TitleErrorMessage).ToBeVisibleAsync();
            await Assertions.Expect(DescriptionErrorMessage).ToBeVisibleAsync();
            await Assertions.Expect(NotesErrorMessage).ToBeVisibleAsync();
        }

        public async Task VerifySuccessAlertAsync()
        {
            await Assertions.Expect(SuccessAlert).ToBeVisibleAsync();
        }

        public async Task VerifySuccessAlertTextAsync(string expectedText)
        {
            await Assertions.Expect(SuccessAlert).ToContainTextAsync(expectedText);
        }

        public async Task FillProductTitleAsync(string text)
        {
            await AddProductTitle.FillAsync(text);
        }

        public async Task FillProductDescriptionAsync(string text)
        {
            await AddProductDescription.FillAsync(text);
        }

        public async Task FillProductUrlAsync(string url)
        {
            await AddProductUrl.FillAsync(url);
        }

        public async Task CheckPhaseAsync(int index)
        {
            if (index == 1) await AddProductPhase1.CheckAsync();
            else if (index == 2) await AddProductPhase2.CheckAsync();
        }

        public async Task CheckBusinessAreaAsync(int index)
        {
            if (index == 1) await AddProductBusinessArea1.CheckAsync();
            else if (index == 2) await AddProductBusinessArea2.CheckAsync();
        }

        public async Task CheckAllChannelsAsync()
        {
            await AddProductChannelCB1.CheckAsync();
            await AddProductChannelCB2.CheckAsync();
            await AddProductChannelCB3.CheckAsync();
            await AddProductChannelCB4.CheckAsync();
            await AddProductChannelCB5.CheckAsync();
            await AddProductChannelCB6.CheckAsync();
            await AddProductChannelCB7.CheckAsync();
            await AddProductChannelCB8.CheckAsync();
            await AddProductChannelCB9.CheckAsync();
        }

        public async Task CheckChannelAsync(int index)
        {
            var channels = new[] { AddProductChannelCB1, AddProductChannelCB2, AddProductChannelCB3,
                AddProductChannelCB4, AddProductChannelCB5, AddProductChannelCB6,
                AddProductChannelCB7, AddProductChannelCB8, AddProductChannelCB9 };
            if (index >= 1 && index <= channels.Length)
                await channels[index - 1].CheckAsync();
        }

        public async Task CheckAllTypesAsync()
        {
            await AddProductTypeCB1.CheckAsync();
            await AddProductTypeCB2.CheckAsync();
            await AddProductTypeCB3.CheckAsync();
            await AddProductTypeCB4.CheckAsync();
            await AddProductTypeCB5.CheckAsync();
        }

        public async Task FillAdditionalInfoAsync(string text)
        {
            await AddAdditionalInfo.FillAsync(text);
        }

        public async Task FillDeliveryManagerAsync(string text)
        {
            await AddDeliveryManager.FillAsync(text);
        }

        public async Task FillProductManagerAsync(string text)
        {
            await AddProductManager.FillAsync(text);
        }

        public async Task FillSeniorRespOfficerAsync(string text)
        {
            await AddSeniorRespOfficer.FillAsync(text);
        }

        public async Task FillNotesAsync(string text)
        {
            await AddNotesTextbox.FillAsync(text);
        }

        public async Task VerifyNotesHintAsync(string expectedText)
        {
            await Assertions.Expect(NotesHintText).ToHaveTextAsync(expectedText);
        }

        public async Task VerifyFormVisibleAsync()
        {
            await Assertions.Expect(RequestForm).ToBeVisibleAsync();
        }

        public async Task VerifyErrorMessageBoxAsync(string expectedText)
        {
            await Assertions.Expect(ErrorMessageBox).ToContainTextAsync(expectedText);
        }

        public async Task VerifyTitleErrorAsync(string expectedText)
        {
            await Assertions.Expect(TitleErrorMessage).ToHaveTextAsync(expectedText);
        }

        public async Task VerifyDescriptionErrorAsync(string expectedText)
        {
            await Assertions.Expect(DescriptionErrorMessage).ToHaveTextAsync(expectedText);
        }

        public async Task VerifyNotesErrorAsync(string expectedText)
        {
            await Assertions.Expect(NotesErrorMessage).ToHaveTextAsync(expectedText);
        }

        public async Task ClickTitleErrorLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Please provide a product title" }).ClickAsync();
        }

        public async Task ClickDescriptionErrorLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Please provide a description" }).ClickAsync();
        }

        public async Task ClickNotesErrorLinkAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Please provide notes about this request" }).ClickAsync();
        }

        public async Task VerifyTitleFocusedAsync()
        {
            await Assertions.Expect(AddProductTitle).ToBeFocusedAsync();
        }

        public async Task VerifyDescriptionFocusedAsync()
        {
            await Assertions.Expect(AddProductDescription).ToBeFocusedAsync();
        }

        public async Task VerifyNotesFocusedAsync()
        {
            await Assertions.Expect(AddNotesTextbox).ToBeFocusedAsync();
        }

        public async Task CancelFormAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Cancel" }).ClickAsync();
        }
    }
}
